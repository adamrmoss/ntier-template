using Microsoft.Extensions.DependencyInjection;
using NTierTemplate.Application.Users;

namespace NTierTemplate.Application.Ioc;

/// <summary>
/// Registers application services with the dependency injection container.
/// </summary>
public static class ServiceRegistrar
{
    /// <summary>
    /// Register application service interfaces and their implementations as scoped services.
    /// </summary>
    /// <param name="serviceCollection">The service collection to register services with.</param>
    public static void RegisterServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IUserApplicationService, UserApplicationService>();
    }
}
