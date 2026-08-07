using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NTierTemplate.Application.Ioc;

/// <summary>
/// Registers queue command services with the dependency injection container.
/// </summary>
public static class QueueRegistrar
{
    /// <summary>
    /// Register RabbitMQ options and <see cref="Queue.IQueueApplicationService"/>.
    /// </summary>
    /// <param name="serviceCollection">The service collection to register queue services with.</param>
    /// <param name="configuration">Host configuration.</param>
    public static void RegisterQueue(
        this IServiceCollection serviceCollection,
        IConfiguration configuration
    )
    {
        serviceCollection.Configure<Queue.RabbitMqOptions>(configuration.GetSection(Queue.RabbitMqOptions.SectionName));
        serviceCollection.AddSingleton<Queue.QueueApplicationService>();
        serviceCollection.AddSingleton<Queue.IQueueApplicationService>(serviceProvider =>
            serviceProvider.GetRequiredService<Queue.QueueApplicationService>());
    }
}
