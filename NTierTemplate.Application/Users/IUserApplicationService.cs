using NTierTemplate.Users;

namespace NTierTemplate.Application.Users;

/// <summary>
/// Application use cases for account users.
/// </summary>
public interface IUserApplicationService
{
    /// <summary>
    /// Ensure default application roles exist.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task EnsureDefaultRolesExistAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Register a new standard user account.
    /// </summary>
    /// <param name="request">Registration payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Creation outcome with the new user when successful.</returns>
    Task<UserCreateResult> RegisterAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Create an administrator account.
    /// </summary>
    /// <param name="email">Administrator email.</param>
    /// <param name="password">Administrator password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Creation outcome with the new user when successful.</returns>
    Task<UserCreateResult> CreateAdminAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Retrieve a user by ID.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user if found, otherwise null.</returns>
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve a user by email address.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user if found, otherwise null.</returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve the authenticated user for the current scope, when one is available.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current user, otherwise null.</returns>
    Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate an email and password and return the matching user when credentials are valid.
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
    /// Generate an email confirmation token for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The token when the user exists; otherwise null.</returns>
    Task<string?> GenerateEmailConfirmationTokenAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirm a user's email address with an Identity confirmation token.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="token">The confirmation token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Operation outcome with the user when successful.</returns>
    Task<AuthOperationResult> ConfirmEmailAsync(
        int userId,
        string token,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Generate a password reset token for an email address.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The token when the account exists; otherwise null.</returns>
    Task<string?> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset a user's password with an Identity reset token.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="token">The reset token.</param>
    /// <param name="newPassword">The new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Operation outcome with the user when successful.</returns>
    Task<AuthOperationResult> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default
    );
}
