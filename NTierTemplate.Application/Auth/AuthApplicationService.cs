using System.Security.Cryptography;
using System.Text;
using NTierTemplate.Application.Users;
using NTierTemplate.Data.Users;
using NTierTemplate.Users;

namespace NTierTemplate.Application.Auth;

/// <summary>
/// Authentication, session, and refresh-token use cases.
/// </summary>
public class AuthApplicationService(
    IUserDao userDao,
    IRefreshTokenDao refreshTokenDao,
    IPrincipalContainer principalContainer
)
    : IAuthApplicationService
{
    /// <inheritdoc />
    public async Task<User?> ValidatePasswordAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default
    )
    {
        // Validate credentials against Identity.
        var user = await userDao.ValidatePasswordAsync(email, password, cancellationToken);

        // Establish the scoped principal when sign-in succeeds.
        if (user is not null)
        {
            principalContainer.SignIn(user);
        }

        return user;
    }

    /// <inheritdoc />
    public Task<bool> CheckPasswordAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default
    )
    {
        return userDao.CheckPasswordAsync(email, password, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> IsEmailConfirmedAsync(string email, CancellationToken cancellationToken = default)
    {
        return userDao.IsEmailConfirmedAsync(email, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var principal = principalContainer.Principal;

        // Return early when the scope has no authenticated principal.
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        // Resolve the domain user from the authenticated principal.
        return await userDao.GetByPrincipalAsync(principal, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> IssueRefreshTokenAsync(
        int userId,
        int refreshTokenDays,
        CancellationToken cancellationToken = default
    )
    {
        // Generate a raw token and store only its hash.
        var rawToken = GenerateSecureToken();
        var tokenHash = HashToken(rawToken);
        var expiresAt = DateTime.UtcNow.AddDays(refreshTokenDays);

        await refreshTokenDao.CreateAsync(userId, tokenHash, expiresAt, cancellationToken);

        return rawToken;
    }

    /// <inheritdoc />
    public async Task<int?> RedeemRefreshTokenAsync(
        string rawToken,
        CancellationToken cancellationToken = default
    )
    {
        // Look up a valid token by hash.
        var tokenHash = HashToken(rawToken);
        var userId = await refreshTokenDao.FindValidUserIdByTokenHashAsync(tokenHash, cancellationToken);

        if (userId == null)
        {
            return null;
        }

        // Rotate by revoking the token that was just redeemed.
        await refreshTokenDao.RevokeByTokenHashAsync(tokenHash, cancellationToken);

        return userId;
    }

    /// <inheritdoc />
    public Task RevokeRefreshTokenAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        return refreshTokenDao.RevokeByTokenHashAsync(HashToken(rawToken), cancellationToken);
    }

    private static string GenerateSecureToken()
    {
        // Generate cryptographically random token bytes.
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string token)
    {
        // Hash the raw token for storage and lookup.
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
