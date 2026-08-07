using NTierTemplate.Users;

namespace NTierTemplate.Application.Auth;

/// <summary>
/// Authentication, session, and refresh-token use cases: sign-in, credential checks used
/// during login, current-user resolution, and refresh-token issue/redeem/revoke.
/// Account registration, password reset, email confirmation, and transactional email
/// belong on <see cref="Users.IUserApplicationService"/>. JWT access tokens are issued in the Api host.
/// </summary>
public interface IAuthApplicationService
{
    /// <summary>
    /// Validate an email and password and establish the current user when credentials are valid.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="password">The password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user when credentials are valid, otherwise null.</returns>
    Task<User?> ValidatePasswordAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Check whether an email and password match without requiring email confirmation.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="password">The password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True when credentials match.</returns>
    Task<bool> CheckPasswordAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Determine whether the account for an email address has a confirmed email.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True when the account exists and email is confirmed.</returns>
    Task<bool> IsEmailConfirmedAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve the authenticated user for the current scope, when one is available.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current user, otherwise null.</returns>
    Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Issue a new refresh token for a signed-in user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="refreshTokenDays">Days until the refresh token expires.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The raw refresh token.</returns>
    Task<string> IssueRefreshTokenAsync(
        int userId,
        int refreshTokenDays,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Validate and revoke a refresh token, returning the associated user ID when valid.
    /// </summary>
    /// <param name="rawToken">The raw refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user ID when valid; otherwise null.</returns>
    Task<int?> RedeemRefreshTokenAsync(string rawToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke a refresh token when it exists and has not already been revoked.
    /// </summary>
    /// <param name="rawToken">The raw refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RevokeRefreshTokenAsync(string rawToken, CancellationToken cancellationToken = default);
}
