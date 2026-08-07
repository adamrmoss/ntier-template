using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NTierTemplate.Application.Auth;
using NTierTemplate.Data;
using NTierTemplate.Data.FailedCommands;
using NTierTemplate.Messaging;
using NTierTemplate.Users;
using RabbitMQ.Client;

namespace NTierTemplate.Application.Queue;

/// <summary>
/// Enqueues, processes, and retries domain commands via RabbitMQ.
/// </summary>
public sealed class QueueApplicationService(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> rabbitMqOptions,
    ILogger<QueueApplicationService> logger
) : IQueueApplicationService, IAsyncDisposable
{
    private const int RetryBatchSize = 20;

    private readonly RabbitMqOptions options = rabbitMqOptions.Value;
    private readonly SemaphoreSlim initializationLock = new(1, 1);
    private IConnection? connection;
    private IChannel? channel;

    /// <inheritdoc />
    public async Task<Guid> EnqueueAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : class
    {
        var envelope = new CommandEnvelope
        {
            CommandName = typeof(TCommand).FullName ?? typeof(TCommand).Name,
            Payload = JsonSerializer.Serialize(command),
        };

        await this.PublishAsync(envelope, cancellationToken);

        return envelope.MessageId;
    }

    /// <inheritdoc />
    public async Task ProcessAsync(CommandEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var result = await this.ExecuteAsync(envelope, cancellationToken);

        using var scope = scopeFactory.CreateScope();
        var failedCommandDao = scope.ServiceProvider.GetRequiredService<IFailedCommandDao>();

        if (result.Succeeded)
        {
            await failedCommandDao.MarkSucceededAsync(envelope.MessageId, cancellationToken);
            return;
        }

        await this.ScheduleRetryAsync(
            failedCommandDao,
            envelope,
            result.ErrorMessage ?? "Command processing failed.",
            result.IsPermanentFailure,
            cancellationToken
        );
    }

    /// <inheritdoc />
    public async Task RetryDueAsync(CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var failedCommandDao = scope.ServiceProvider.GetRequiredService<IFailedCommandDao>();

        var dueCommands = await failedCommandDao.GetDueForRetryAsync(
            DateTime.UtcNow,
            RetryBatchSize,
            cancellationToken
        );

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

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (this.channel != null)
        {
            await this.channel.CloseAsync();
            await this.channel.DisposeAsync();
            this.channel = null;
        }

        if (this.connection != null)
        {
            await this.connection.CloseAsync();
            await this.connection.DisposeAsync();
            this.connection = null;
        }

        this.initializationLock.Dispose();
    }

    private async Task<QueueCommandResult> ExecuteAsync(
        CommandEnvelope envelope,
        CancellationToken cancellationToken
    )
    {
        var commandType = ResolveCommandType(envelope.CommandName);

        if (commandType == null)
        {
            return QueueCommandResult.PermanentFailure($"Unknown command type '{envelope.CommandName}'.");
        }

        object? command;

        try
        {
            command = JsonSerializer.Deserialize(envelope.Payload, commandType);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(
                exception,
                "Invalid payload for command type {CommandType} message {MessageId}.",
                envelope.CommandName,
                envelope.MessageId
            );

            return QueueCommandResult.PermanentFailure("Invalid command payload.");
        }

        if (command == null)
        {
            return QueueCommandResult.PermanentFailure("Command payload was empty.");
        }

        if (commandType == typeof(RegisterUserCommand))
        {
            return await this.ProcessRegisterUserAsync((RegisterUserCommand)command, cancellationToken);
        }

        return QueueCommandResult.PermanentFailure($"Unsupported command type '{envelope.CommandName}'.");
    }

    private async Task<QueueCommandResult> ProcessRegisterUserAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken
    )
    {
        using var scope = scopeFactory.CreateScope();
        var authApplicationService = scope.ServiceProvider.GetRequiredService<IAuthApplicationService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);

            var result = await authApplicationService.ProcessRegisterUserAsync(
                command.ToRequest(),
                cancellationToken
            );

            if (!result.Succeeded)
            {
                await unitOfWork.RollbackAsync(cancellationToken);

                return result.IsDuplicateEmail
                    ? QueueCommandResult.PermanentFailure(result.ErrorMessage ?? "Duplicate email.")
                    : QueueCommandResult.TransientFailure(result.ErrorMessage ?? "Registration failed.");
            }

            await unitOfWork.CommitAsync(cancellationToken);

            return QueueCommandResult.Success();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Register user command failed.");

            try
            {
                await unitOfWork.RollbackAsync(cancellationToken);
            }
            catch (Exception rollbackException)
            {
                logger.LogWarning(rollbackException, "Rollback failed after register user error.");
            }

            return QueueCommandResult.TransientFailure(exception.Message);
        }
    }

    private static Type? ResolveCommandType(string commandTypeName)
    {
        return Type.GetType(commandTypeName)
            ?? typeof(RegisterUserCommand).Assembly.GetType(commandTypeName);
    }

    private async Task PublishAsync(CommandEnvelope envelope, CancellationToken cancellationToken)
    {
        await this.EnsureChannelAsync(cancellationToken);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope));
        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = envelope.MessageId.ToString(),
        };

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
        if (this.channel != null)
        {
            return;
        }

        await this.initializationLock.WaitAsync(cancellationToken);

        try
        {
            if (this.channel != null)
            {
                return;
            }

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
        var nextAttempt = envelope.Attempt + 1;
        var exhausted = isPermanentFailure || nextAttempt >= envelope.MaxAttempts;
        var nextRetryAtUtc = exhausted
            ? (DateTime?)null
            : DateTime.UtcNow.Add(GetRetryDelay(nextAttempt));

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
        var seconds = Math.Pow(2, Math.Min(attempt, 6));
        return TimeSpan.FromSeconds(seconds);
    }

    private static string? TruncateError(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            return null;
        }

        return errorMessage.Length <= 2000
            ? errorMessage
            : errorMessage[..2000];
    }

    private sealed class QueueCommandResult
    {
        public bool Succeeded { get; init; }

        public bool IsPermanentFailure { get; init; }

        public string? ErrorMessage { get; init; }

        public static QueueCommandResult Success()
        {
            return new QueueCommandResult { Succeeded = true };
        }

        public static QueueCommandResult PermanentFailure(string errorMessage)
        {
            return new QueueCommandResult
            {
                Succeeded = false,
                IsPermanentFailure = true,
                ErrorMessage = errorMessage,
            };
        }

        public static QueueCommandResult TransientFailure(string errorMessage)
        {
            return new QueueCommandResult
            {
                Succeeded = false,
                IsPermanentFailure = false,
                ErrorMessage = errorMessage,
            };
        }
    }
}
