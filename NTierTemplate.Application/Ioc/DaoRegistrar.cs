using Microsoft.Extensions.DependencyInjection;
using NTierTemplate.Data;
using NTierTemplate.Data.FailedCommands;
using NTierTemplate.Data.Users;

namespace NTierTemplate.Application.Ioc;

/// <summary>
/// Registers data access objects with the dependency injection container.
/// </summary>
public static class DaoRegistrar
{
    /// <summary>
    /// Register all DAO interfaces and their implementations as scoped services.
    /// </summary>
    /// <param name="serviceCollection">The service collection to register DAOs with.</param>
    public static void RegisterDaos(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IUserDao, UserDao>();
        serviceCollection.AddScoped<IRefreshTokenDao, RefreshTokenDao>();
        serviceCollection.AddScoped<IFailedCommandDao, FailedCommandDao>();
        serviceCollection.AddScoped<IUnitOfWork, UnitOfWork>();
    }
}
