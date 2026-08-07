using System.Security.Claims;
using NTierTemplate.Users;

namespace NTierTemplate.Application.Users;

/// <summary>
/// Provides the authenticated principal for the current request scope and records sign-in state.
/// Implemented by entry-point hosts (for example, the API).
/// </summary>
public interface IPrincipalContainer
{
    /// <summary>
    /// The current authenticated principal, when one is available.
    /// </summary>
    ClaimsPrincipal? Principal { get; }

    /// <summary>
    /// Record a successful sign-in for the current scope.
    /// </summary>
    /// <param name="user">The signed-in user.</param>
    void SignIn(User user);
}
