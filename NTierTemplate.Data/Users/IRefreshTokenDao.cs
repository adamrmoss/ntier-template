namespace NTierTemplate.Data.Users;

/// <summary>
/// Data access for rotating refresh tokens.
/// </summary>
public interface IRefreshTokenDao
{
    /// <summary>
    /// Persist a new refresh token hash for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="tokenHash">SHA-256 hash of the raw token.</param>
    /// <param name="expiresAt">UTC expiry time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CreateAsync(
        int userId,
        string tokenHash,
        DateTime expiresAt,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Find the user ID for a valid, non-revoked refresh token hash.
    /// </summary>
    /// <param name="tokenHash">SHA-256 hash of the raw token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user ID when valid; otherwise null.</returns>
    Task<int?> FindValidUserIdByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Revoke a refresh token by hash when it has not already been revoked.
    /// </summary>
    /// <param name="tokenHash">SHA-256 hash of the raw token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RevokeByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
}
