using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NTierTemplate;
using NTierTemplate.Application.Ioc;
using NTierTemplate.Data.FailedCommands;
using NTierTemplate.Messaging;
using RabbitMQ.Client;

namespace NTierTemplate.Application.Queue;

/// <summary>
/// Enqueues, processes, and retries domain commands via RabbitMQ.
/// </summary>
public sealed class QueueApplicationService(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> rabbitMqOptions,
    IOptionsMonitor<JsonSerializerOptions> jsonOptionsMonitor,
    ILogger<QueueApplicationService> logger
)
    : IQueueApplicationService, IHostedService, IAsyncDisposable
{
    private const int RetryBatchSize = 20;

    private static readonly TimeSpan RetryPollInterval = TimeSpan.FromSeconds(30);

    private readonly RabbitMqOptions options = rabbitMqOptions.Value;
    private readonly JsonSerializerOptions pascalCaseJsonOptions =
        jsonOptionsMonitor.Get(SerializerRegistrar.PascalCaseOptionsName);
    private readonly SemaphoreSlim initializationLock = new(1, 1);
    private IConnection? connection;
    private IChannel? channel;
    private CancellationTokenSource? retryLoopCancellation;
    private Task? retryLoopTask;

    /// <inheritdoc />
    public async Task<Guid> EnqueueAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : class
    {
        // Wrap the command in a queue envelope.
        var envelope = new CommandEnvelope
        {
            CommandName = typeof(TCommand).FullName ?? typeof(TCommand).Name,
            Payload = JsonSerializer.Serialize(command, this.pascalCaseJsonOptions),
        };

        // Publish the envelope to RabbitMQ.
        await this.PublishAsync(envelope, cancellationToken);

        return envelope.MessageId;
    }

    /// <inheritdoc />
    public async Task ProcessAsync(CommandEnvelope envelope, CancellationToken cancellationToken = default)
    {
        try
        {
            // Handlers are scoped; resolve them in a per-message scope.
            using var scope = scopeFactory.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            // Execute the command and capture the outcome.
            var result = await this.ExecuteAsync(envelope, serviceProvider, cancellationToken);
            var failedCommandDao = serviceProvider.GetRequiredService<IFailedCommandDao>();

            if (result.Succeeded)
            {
                // Clear any prior failed-command record on success.
                await failedCommandDao.MarkSucceededAsync(envelope.MessageId, cancellationToken);
                return;
            }

            // Schedule a retry or mark the command exhausted.
            await ScheduleRetryAsync(
                failedCommandDao,
                envelope,
                result.ErrorMessage ?? "Command processing failed.",
                result.IsPermanentFailure,
                cancellationToken
            );
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception,
                "Unhandled error processing command {CommandName} message {MessageId}.",
                envelope.CommandName,
                envelope.MessageId
            );

            // Record a transient failure so the retry sweep can republish the command.
            using var scope = scopeFactory.CreateScope();
            var failedCommandDao = scope.ServiceProvider.GetRequiredService<IFailedCommandDao>();

            await ScheduleRetryAsync(
                failedCommandDao,
                envelope,
                exception.Message,
                isPermanentFailure: false,
                cancellationToken
            );
        }
    }

    /// <inheritdoc />
    public async Task RetryDueAsync(CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var failedCommandDao = scope.ServiceProvider.GetRequiredService<IFailedCommandDao>();

        // Load failed commands that are due for another attempt.
        var dueCommands = await failedCommandDao.GetDueForRetryAsync(
            DateTime.UtcNow,
            RetryBatchSize,
            cancellationToken
        );

        // Republish each due command to the work queue.
        foreach (var failedCommand in dueCommands)
        {
            var envelope = new CommandEnvelope
            {
                MessageId = failedCommand.MessageId,
                CommandName = failedCommand.CommandName,
                Payload = failedCommand.Payload,
                Attempt = failedCommand.AttemptCount,
                MaxAttempts = failedCommand.MaxAttempts,
                CreatedAtUtc = DateTime.UtcNow,
            };

            await this.PublishAsync(envelope, cancellationToken);
        }
    }

    /// <summary>
    /// Start the failed-command retry sweep loop when registered as a hosted service.
    /// </summary>
    /// <param name="cancellationToken">Host shutdown token.</param>
    /// <returns>A completed task once the background loop has been scheduled.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Run only when the host registers this type as IHostedService (Queue worker).
        this.retryLoopCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        this.retryLoopTask = this.RunRetryLoopAsync(this.retryLoopCancellation.Token);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stop the failed-command retry sweep loop.
    /// </summary>
    /// <param name="cancellationToken">Host shutdown token.</param>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (this.retryLoopCancellation == null)
        {
            return;
        }

        // Cancel the loop and wait for the in-flight sweep to finish.
        await this.retryLoopCancellation.CancelAsync();

        if (this.retryLoopTask != null)
        {
            await this.retryLoopTask.WaitAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // Stop the retry loop when the singleton is disposed (for example in tests).
        if (this.retryLoopCancellation != null)
        {
            await this.retryLoopCancellation.CancelAsync();

            if (this.retryLoopTask != null)
            {
                try
                {
                    await this.retryLoopTask;
                }
                catch (OperationCanceledException)
                {
                }
            }

            this.retryLoopCancellation.Dispose();
            this.retryLoopCancellation = null;
            this.retryLoopTask = null;
        }

        // Close and dispose the open channel.
        if (this.channel != null)
        {
            await this.channel.CloseAsync();
            await this.channel.DisposeAsync();
            this.channel = null;
        }

        // Close and dispose the open connection.
        if (this.connection != null)
        {
            await this.connection.CloseAsync();
            await this.connection.DisposeAsync();
            this.connection = null;
        }

        this.initializationLock.Dispose();
    }

    private async Task RunRetryLoopAsync(CancellationToken stoppingToken)
    {
        // Poll for failed commands due for retry until shutdown.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Republish commands that have reached their retry time.
                await this.RetryDueAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Failed command retry sweep failed.");
            }

            try
            {
                // Wait before the next retry sweep.
                await Task.Delay(RetryPollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task<CommandHandlerResult> ExecuteAsync(
        CommandEnvelope envelope,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken
    )
    {
        // Resolve the command type from the envelope name.
        var commandType = ResolveCommandType(envelope.CommandName);

        if (commandType == null)
        {
            // Unknown types cannot be retried — the message will never deserialize.
            return CommandHandlerResult.PermanentFailure($"Unknown command type '{envelope.CommandName}'.");
        }

        object? command;

        try
        {
            // Deserialize the payload into the resolved command type.
            command = JsonSerializer.Deserialize(envelope.Payload, commandType, this.pascalCaseJsonOptions);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(
                exception,
                "Invalid payload for command type {CommandType} message {MessageId}.",
                envelope.CommandName,
                envelope.MessageId
            );

            return CommandHandlerResult.PermanentFailure("Invalid command payload.");
        }

        if (command == null)
        {
            // Empty JSON object or null literal — treat as permanent bad payload.
            return CommandHandlerResult.PermanentFailure("Command payload was empty.");
        }

        // Dispatch to the handler registered for the resolved command type.
        var handler = serviceProvider
            .GetServices<ICommandHandler>()
            .FirstOrDefault(candidate => candidate.CommandType == commandType);

        if (handler == null)
        {
            // Type resolved but no handler registered in this host (misconfiguration).
            return CommandHandlerResult.PermanentFailure(
                $"No handler registered for command type '{envelope.CommandName}'."
            );
        }

        // Run the domain-specific handler for this command type.
        return await handler.HandleAsync(command, cancellationToken);
    }

    private static Type? ResolveCommandType(string commandTypeName)
    {
        // Prefer the stable full name written by EnqueueAsync today.
        var commandType = typeof(AssemblyMarker).Assembly.GetType(commandTypeName);

        if (commandType != null)
        {
            return commandType;
        }

        // Fall back to assembly-qualified names from older queued messages.
        return Type.GetType(commandTypeName, throwOnError: false);
    }

    private async Task PublishAsync(CommandEnvelope envelope, CancellationToken cancellationToken)
    {
        // Ensure a RabbitMQ channel exists before publishing.
        await this.EnsureChannelAsync(cancellationToken);

        // Serialize the envelope and set persistent delivery properties.
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope, this.pascalCaseJsonOptions));
        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = envelope.MessageId.ToString(),
        };

        // Publish to the configured work queue.
        await this.channel!.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: this.options.QueueName,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken
        );
    }

    private async Task EnsureChannelAsync(CancellationToken cancellationToken)
    {
        // Return when the channel is already initialized.
        if (this.channel != null)
        {
            return;
        }

        await this.initializationLock.WaitAsync(cancellationToken);

        try
        {
            // Re-check after acquiring the initialization lock.
            if (this.channel != null)
            {
                return;
            }

            // Open a RabbitMQ connection and channel.
            var factory = new ConnectionFactory
            {
                HostName = this.options.Host,
                Port = this.options.Port,
                UserName = this.options.UserName,
                Password = this.options.Password,
                VirtualHost = this.options.VirtualHost,
            };

            this.connection = await factory.CreateConnectionAsync(cancellationToken);
            this.channel = await this.connection.CreateChannelAsync(cancellationToken: cancellationToken);

            // Declare the work queue as durable.
            await this.channel.QueueDeclareAsync(
                queue: this.options.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken
            );
        }
        finally
        {
            this.initializationLock.Release();
        }
    }

    private static async Task ScheduleRetryAsync(
        IFailedCommandDao failedCommandDao,
        CommandEnvelope envelope,
        string errorMessage,
        bool isPermanentFailure,
        CancellationToken cancellationToken
    )
    {
        // Compute the next attempt number and retry schedule.
        var nextAttempt = envelope.Attempt + 1;
        var exhausted = isPermanentFailure || nextAttempt >= envelope.MaxAttempts;
        var nextRetryAtUtc = exhausted
            ? (DateTime?)null
            : DateTime.UtcNow.Add(GetRetryDelay(nextAttempt));

        // Persist updated retry metadata for the failed command.
        await failedCommandDao.UpsertAsync(
            new FailedCommand
            {
                MessageId = envelope.MessageId,
                CommandName = envelope.CommandName,
                Payload = envelope.Payload,
                AttemptCount = nextAttempt,
                MaxAttempts = envelope.MaxAttempts,
                LastError = TruncateError(errorMessage),
                Status = exhausted ? FailedCommandStatus.Exhausted : FailedCommandStatus.PendingRetry,
                FailedAtUtc = DateTime.UtcNow,
                NextRetryAtUtc = nextRetryAtUtc,
            },
            cancellationToken
        );
    }

    private static TimeSpan GetRetryDelay(int attempt)
    {
        // Use exponential backoff capped at 2^6 seconds.
        var seconds = Math.Pow(2, Math.Min(attempt, 6));
        return TimeSpan.FromSeconds(seconds);
    }

    private static string? TruncateError(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            return null;
        }

        // Limit stored error text to the column capacity.
        return errorMessage.Length <= 2000
            ? errorMessage
            : errorMessage[..2000];
    }
}
