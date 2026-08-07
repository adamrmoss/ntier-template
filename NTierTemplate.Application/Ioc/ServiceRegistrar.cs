using Microsoft.Extensions.DependencyInjection;
using NTierTemplate.Application.Auth;
using NTierTemplate.Application.Email;
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
        serviceCollection.AddScoped<IEmailClient, SmtpEmailClient>();
        serviceCollection.AddScoped<IAuthEmailService, AuthEmailService>();
        serviceCollection.AddScoped<IAuthApplicationService, AuthApplicationService>();
        serviceCollection.AddScoped<IUserApplicationService, UserApplicationService>();
    }
}
