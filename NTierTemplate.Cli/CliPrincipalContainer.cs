using System.Security.Claims;
using NTierTemplate.Application.Users;
using NTierTemplate.Users;

namespace NTierTemplate.Cli;

/// <summary>
/// Scoped principal container for a single CLI command execution.
/// </summary>
public sealed class CliPrincipalContainer : IPrincipalContainer
{
    private ClaimsPrincipal? principal;

    /// <inheritdoc />
    public ClaimsPrincipal? Principal => this.principal;

    /// <inheritdoc />
    public void SignIn(User user)
    {
        this.principal = UserPrincipal.Create(user, authenticationType: "password");
    }
}
