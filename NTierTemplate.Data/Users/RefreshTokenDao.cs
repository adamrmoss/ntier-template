using Microsoft.EntityFrameworkCore;

namespace NTierTemplate.Data.Users;

/// <summary>
/// Data access for rotating refresh tokens.
/// </summary>
public class RefreshTokenDao(NTierTemplateDbContext dbContext)
    : DaoBase, IRefreshTokenDao
{
    /// <inheritdoc />
    public async Task CreateAsync(
        int userId,
        string tokenHash,
        DateTime expiresAt,
        CancellationToken cancellationToken = default
    )
    {
        // Stage a new refresh token row.
        dbContext.RefreshToken.Add(
            new RefreshToken
            {
                UserId = userId,
                TokenHash = tokenHash,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
            }
        );

        // Persist the token to the database.
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int?> FindValidUserIdByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default
    )
    {
        var storedToken = await dbContext.RefreshToken
            .AsNoTracking()
            .SingleOrDefaultAsync(
                token => token.TokenHash == tokenHash
                    && token.RevokedAt == null
                    && token.ExpiresAt > DateTime.UtcNow,
                cancellationToken
            );

        return storedToken?.UserId;
    }

    /// <inheritdoc />
    public async Task RevokeByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        // Load the active token matching the hash.
        var storedToken = await dbContext.RefreshToken
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        // Ignore missing or already-revoked tokens.
        if (storedToken == null || storedToken.RevokedAt != null)
        {
            return;
        }

        // Mark the token revoked and persist the change.
        storedToken.RevokedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
