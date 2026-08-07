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
        using var scope = services.CreateScope();
        var userApplicationService = scope.ServiceProvider.GetRequiredService<IUserApplicationService>();

        await userApplicationService.EnsureDefaultRolesExistAsync(cancellationToken);
    }
}
