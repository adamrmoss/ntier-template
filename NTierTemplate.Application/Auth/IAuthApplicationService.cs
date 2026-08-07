using NTierTemplate.Users;

namespace NTierTemplate.Application.Auth;

/// <summary>
/// Authentication use cases shared across entry-point hosts.
/// </summary>
public interface IAuthApplicationService
{
    /// <summary>
    /// Register a user and send a confirmation email when registration succeeds.
    /// </summary>
    /// <param name="request">Registration payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Registration and email delivery outcome.</returns>
    Task<RegistrationResult> RegisterAndSendConfirmationAsync(
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
