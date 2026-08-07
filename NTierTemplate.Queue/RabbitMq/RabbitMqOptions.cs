namespace NTierTemplate.Queue.RabbitMq;

/// <summary>
/// RabbitMQ connection and queue settings for the worker host.
/// </summary>
public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string UserName { get; set; } = "ntier";

    public string Password { get; set; } = string.Empty;

    public string VirtualHost { get; set; } = "/";

    public string QueueName { get; set; } = "ntier-template";
}
