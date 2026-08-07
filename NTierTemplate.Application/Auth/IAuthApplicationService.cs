using NTierTemplate.Users;

namespace NTierTemplate.Application.Auth;

/// <summary>
/// Authentication use cases shared across entry-point hosts.
/// </summary>
public interface IAuthApplicationService
{
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
    /// Register a user and send a confirmation email when registration succeeds.
    /// Intended for queue command handlers.
    /// </summary>
    /// <param name="request">Registration payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Processing outcome.</returns>
    Task<ProcessRegisterUserResult> ProcessRegisterUserAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default
    );

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
}
