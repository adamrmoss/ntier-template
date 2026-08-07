using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NTierTemplate.Application.Ioc;
using NTierTemplate.Application.Queue;
using NTierTemplate.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NTierTemplate.Queue.RabbitMq;

/// <summary>
/// Background worker that consumes command messages from RabbitMQ one at a time.
/// </summary>
public class QueueConsumerService(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> rabbitMqOptions,
    IOptionsMonitor<JsonSerializerOptions> jsonOptionsMonitor,
    ILogger<QueueConsumerService> logger
)
    : BackgroundService
{
    private readonly RabbitMqOptions options = rabbitMqOptions.Value;
    private readonly JsonSerializerOptions pascalCaseJsonOptions =
        jsonOptionsMonitor.Get(SerializerRegistrar.PascalCaseOptionsName);
    private readonly SemaphoreSlim processingLock = new(1, 1);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Configure the RabbitMQ connection factory.
        var factory = new ConnectionFactory
        {
            HostName = this.options.Host,
            Port = this.options.Port,
            UserName = this.options.UserName,
            Password = this.options.Password,
            VirtualHost = this.options.VirtualHost,
        };

        // Open a connection and channel for consuming.
        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        // Declare the work queue as durable.
        await channel.QueueDeclareAsync(
            queue: this.options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken
        );

        // Limit the broker to one unacknowledged delivery per consumer.
        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: stoppingToken
        );

        // Route incoming deliveries to the message handler.
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, eventArgs) =>
            this.HandleMessageAsync(channel, eventArgs, stoppingToken);

        // Start consuming from the work queue.
        await channel.BasicConsumeAsync(
            queue: this.options.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken
        );

        logger.LogInformation(
            "Queue worker listening sequentially on {QueueName} at {Host}:{Port}.",
            this.options.QueueName,
            this.options.Host,
            this.options.Port
        );

        // Keep the background service alive until shutdown.
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    private async Task HandleMessageAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        CancellationToken cancellationToken
    )
    {
        // Process one command at a time within this worker host.
        await this.processingLock.WaitAsync(cancellationToken);

        try
        {
            // Decode the message body as UTF-8 text.
            var body = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            CommandEnvelope? envelope;

            try
            {
                // Deserialize the command envelope from PascalCase JSON.
                envelope = JsonSerializer.Deserialize<CommandEnvelope>(body, this.pascalCaseJsonOptions);
            }
            catch (JsonException exception)
            {
                logger.LogError(exception, "Queue message was not a valid command envelope.");
                await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken);
                return;
            }

            // Reject envelopes that are empty or missing a command type.
            if (envelope == null || string.IsNullOrWhiteSpace(envelope.CommandName))
            {
                logger.LogWarning("Queue message envelope was empty or missing a command type name.");
                await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken);
                return;
            }

            try
            {
                // Delegate command execution to the application layer.
                using var scope = scopeFactory.CreateScope();
                var queueApplicationService = scope.ServiceProvider.GetRequiredService<IQueueApplicationService>();

                await queueApplicationService.ProcessAsync(envelope, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Unhandled error processing command {CommandName} message {MessageId}.",
                    envelope.CommandName,
                    envelope.MessageId
                );
            }

            // Acknowledge after processing so poison messages do not loop at the broker.
            await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken);
        }
        finally
        {
            this.processingLock.Release();
        }
    }
}
