using System.Security.Claims;

namespace NTierTemplate.Users;

/// <summary>
/// Builds a claims principal for a NTierTemplate account user.
/// </summary>
public static class UserPrincipal
{
    /// <summary>
    /// Create an authenticated principal for the user.
    /// </summary>
    /// <param name="user">The domain user.</param>
    /// <param name="authenticationType">Authentication scheme name.</param>
    /// <returns>Authenticated principal.</returns>
    public static ClaimsPrincipal Create(User user, string authenticationType)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
        };

        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, authenticationType);

        return new ClaimsPrincipal(identity);
    }
}
