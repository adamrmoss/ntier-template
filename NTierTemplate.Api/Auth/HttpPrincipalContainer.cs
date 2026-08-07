using System.Security.Claims;
using NTierTemplate.Application.Users;
using NTierTemplate.Users;
using Microsoft.AspNetCore.Http;

namespace NTierTemplate.Api.Auth;

/// <summary>
/// Reads the authenticated principal scoped to the current HTTP request.
/// </summary>
public sealed class HttpPrincipalContainer(IHttpContextAccessor httpContextAccessor)
    : IPrincipalContainer
{
    /// <inheritdoc />
    public ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public void SignIn(User user)
    {
        // JWT middleware establishes the principal on authenticated API requests.
    }
}
