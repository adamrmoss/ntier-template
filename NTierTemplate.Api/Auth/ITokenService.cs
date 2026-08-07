using NTierTemplate.Users;

namespace NTierTemplate.Api.Auth;

/// <summary>
/// Issues and validates JWT access tokens and refresh tokens.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Create access and refresh tokens for a signed-in user.
    /// </summary>
    /// <param name="user">The authenticated user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Token response.</returns>
    Task<TokenResponse> CreateTokenPairAsync(
        User user,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Rotate a refresh token and issue a new access token.
    /// </summary>
    /// <param name="refreshToken">The raw refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Token response when valid; otherwise null.</returns>
    Task<TokenResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke a refresh token if it exists.
    /// </summary>
    /// <param name="refreshToken">The raw refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}
