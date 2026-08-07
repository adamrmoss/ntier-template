namespace NTierTemplate.Application.Email;

/// <summary>
/// Sends outbound email messages.
/// </summary>
public interface IEmailClient
{
    /// <summary>
    /// Send an email message.
    /// </summary>
    /// <param name="message">Recipient, subject, and body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
