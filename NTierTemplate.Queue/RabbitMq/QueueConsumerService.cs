using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NTierTemplate.Queue.RabbitMq;

/// <summary>
/// Background worker that consumes command messages from RabbitMQ.
/// </summary>
public class QueueConsumerService(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> rabbitMqOptions,
    ILogger<QueueConsumerService> logger
)
    : BackgroundService
{
    private readonly RabbitMqOptions options = rabbitMqOptions.Value;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = this.options.Host,
            Port = this.options.Port,
            UserName = this.options.UserName,
            Password = this.options.Password,
            VirtualHost = this.options.VirtualHost,
        };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: this.options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken
        );

        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: stoppingToken
        );

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, eventArgs) =>
            this.HandleMessageAsync(channel, eventArgs, stoppingToken);

        await channel.BasicConsumeAsync(
            queue: this.options.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken
        );

        logger.LogInformation(
            "Queue worker listening on {QueueName} at {Host}:{Port}.",
            this.options.QueueName,
            this.options.Host,
            this.options.Port
        );

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    private async Task HandleMessageAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        CancellationToken cancellationToken
    )
    {
        var body = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

        try
        {
            using var scope = scopeFactory.CreateScope();

            logger.LogInformation(
                "Received message on {RoutingKey}: {Body}",
                eventArgs.RoutingKey,
                body
            );

            // Deserialize domain command contracts and invoke Application services here.

            await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to process queue message.");

            await channel.BasicNackAsync(
                eventArgs.DeliveryTag,
                multiple: false,
                requeue: false,
                cancellationToken
            );
        }
    }
}
