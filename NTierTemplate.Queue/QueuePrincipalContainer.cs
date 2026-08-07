using System.Security.Claims;
using NTierTemplate.Application.Users;
using NTierTemplate.Users;

namespace NTierTemplate.Queue;

/// <summary>
/// Worker host implementation of <see cref="IPrincipalContainer"/> without an authenticated principal.
/// </summary>
public sealed class QueuePrincipalContainer : IPrincipalContainer
{
    /// <inheritdoc />
    public ClaimsPrincipal? Principal => null;

    /// <inheritdoc />
    public void SignIn(User user)
    {
    }
}
