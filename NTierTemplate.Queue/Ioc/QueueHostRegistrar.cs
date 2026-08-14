using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NTierTemplate.Application.Queue;

namespace NTierTemplate.Queue.Ioc;

/// <summary>
/// Registers Queue worker hosted services beyond shared Application wiring.
/// </summary>
public static class QueueHostRegistrar
{
    /// <summary>
    /// Start the failed-command retry sweep on the shared <see cref="QueueApplicationService"/> singleton.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    public static void RegisterQueueWorkerHostedServices(this IServiceCollection serviceCollection)
    {
        // Reuse the IQueueApplicationService singleton so retry shares the publish channel.
        serviceCollection.AddHostedService(serviceProvider =>
            serviceProvider.GetRequiredService<QueueApplicationService>());
    }
}
