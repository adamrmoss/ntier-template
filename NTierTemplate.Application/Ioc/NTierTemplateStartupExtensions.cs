using Microsoft.Extensions.DependencyInjection;
using NTierTemplate.Application.Users;

namespace NTierTemplate.Application.Ioc;

/// <summary>
/// Startup tasks shared across entry-point hosts.
/// </summary>
public static class NTierTemplateStartupExtensions
{
    /// <summary>
    /// Ensure default application roles exist before serving traffic or running commands.
    /// </summary>
    /// <param name="services">Application services.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task EnsureDefaultRolesAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default
    )
    {
        // Resolve user services in a short-lived scope.
        using var scope = services.CreateScope();
        var userApplicationService = scope.ServiceProvider.GetRequiredService<IUserApplicationService>();

        // Ensure default roles exist in the database.
        await userApplicationService.EnsureDefaultRolesExistAsync(cancellationToken);
    }
}
