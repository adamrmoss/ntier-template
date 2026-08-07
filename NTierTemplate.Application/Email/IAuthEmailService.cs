using NTierTemplate.Users;

namespace NTierTemplate.Application.Email;

/// <summary>
/// Composes and sends authentication-related transactional email.
/// </summary>
public interface IAuthEmailService
{
    /// <summary>
    /// Send an email-address confirmation link to a new account.
    /// </summary>
    /// <param name="user">The registered user.</param>
    /// <param name="confirmationToken">Identity email confirmation token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendEmailConfirmationAsync(
        User user,
        string confirmationToken,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Send a password reset link.
    /// </summary>
    /// <param name="email">Recipient email address.</param>
    /// <param name="resetToken">Identity password reset token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendPasswordResetAsync(
        string email,
        string resetToken,
        CancellationToken cancellationToken = default
    );
}
