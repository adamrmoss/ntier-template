using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NTierTemplate.Application.Queue;

namespace NTierTemplate.Application.Ioc;

/// <summary>
/// Registers queue command services with the dependency injection container.
/// </summary>
public static class QueueRegistrar
{
    /// <summary>
    /// Register RabbitMQ options and <see cref="IQueueApplicationService"/>.
    /// </summary>
    /// <param name="serviceCollection">The service collection to register queue services with.</param>
    /// <param name="configuration">Host configuration.</param>
    public static void RegisterQueue(
        this IServiceCollection serviceCollection,
        IConfiguration configuration
    )
    {
        serviceCollection.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        serviceCollection.AddSingleton<IQueueApplicationService, QueueApplicationService>();
    }
}
