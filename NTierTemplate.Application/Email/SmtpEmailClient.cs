using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace NTierTemplate.Application.Email;

/// <summary>
/// Sends email via configured SMTP settings.
/// </summary>
public class SmtpEmailClient(IOptions<SmtpOptions> smtpOptions)
    : IEmailClient
{
    private readonly SmtpOptions options = smtpOptions.Value;

    /// <inheritdoc />
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        using var mailMessage = new MailMessage
        {
            From = new MailAddress(this.options.FromAddress, this.options.FromDisplayName),
            Subject = message.Subject,
            Body = message.PlainTextBody,
            IsBodyHtml = message.HtmlBody != null,
        };

        mailMessage.To.Add(message.ToAddress);

        if (message.HtmlBody != null)
        {
            mailMessage.AlternateViews.Add(
                AlternateView.CreateAlternateViewFromString(message.HtmlBody, null, "text/html")
            );
        }

        using var client = new SmtpClient(this.options.Host, this.options.Port)
        {
            Credentials = new NetworkCredential(this.options.Username, this.options.Password),
            EnableSsl = true,
        };

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(mailMessage, cancellationToken);
    }
}
