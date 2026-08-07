using NTierTemplate.Users;

namespace NTierTemplate.Application.Users;

/// <summary>
/// User account maintenance, registration, and transactional email: account creation,
/// profile lookups, email confirmation and password reset flows, and queue-backed registration.
/// Sign-in, session, refresh tokens, and current-user resolution belong on
/// <see cref="Auth.IAuthApplicationService"/>.
/// </summary>
public interface IUserApplicationService
{
    /// <summary>
    /// Ensure default application roles exist.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task EnsureDefaultRolesExistAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Enqueue a register-user command for asynchronous processing.
    /// </summary>
    /// <param name="request">Registration payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The queued message identifier.</returns>
    Task<Guid> EnqueueRegisterUserAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Register a user and send a confirmation email inside a transaction boundary.
    /// Intended for queue command handlers.
    /// </summary>
    /// <param name="request">Registration payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Processing outcome, including whether the failure was caused by a duplicate email.</returns>
    Task<ProcessRegisterUserResult> ProcessRegisterUserAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default
    );

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
    /// Resend a confirmation email when an unconfirmed account exists for the address.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ResendConfirmationEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a password reset email when an account exists for the address.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendPasswordResetEmailAsync(string email, CancellationToken cancellationToken = default);

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
