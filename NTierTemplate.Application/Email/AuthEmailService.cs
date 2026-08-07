using Microsoft.Extensions.Options;
using NTierTemplate.Users;

namespace NTierTemplate.Application.Email;

/// <summary>
/// Composes and sends authentication-related transactional email.
/// </summary>
public class AuthEmailService(
    IEmailClient emailClient,
    IOptions<AppOptions> appOptions
)
    : IAuthEmailService
{
    private readonly AppOptions options = appOptions.Value;

    /// <inheritdoc />
    public Task SendEmailConfirmationAsync(
        User user,
        string confirmationToken,
        CancellationToken cancellationToken = default
    )
    {
        var encodedToken = Uri.EscapeDataString(confirmationToken);
        var link = $"{this.options.FrontendBaseUrl.TrimEnd('/')}/confirm-email?userId={user.Id}&token={encodedToken}";
        var subject = "Confirm your account";
        var plainText =
            $"Welcome.\n\nConfirm your email address by opening this link:\n{link}\n\nIf you did not create an account, you can ignore this message.";
        var htmlBody = $"""
            <p>Welcome.</p>
            <p><a href="{link}">Confirm your email address</a></p>
            <p>If you did not create an account, you can ignore this message.</p>
            """;

        return emailClient.SendAsync(
            new EmailMessage
            {
                ToAddress = user.Email,
                Subject = subject,
                PlainTextBody = plainText,
                HtmlBody = htmlBody,
            },
            cancellationToken
        );
    }

    /// <inheritdoc />
    public Task SendPasswordResetAsync(
        string email,
        string resetToken,
        CancellationToken cancellationToken = default
    )
    {
        var encodedEmail = Uri.EscapeDataString(email);
        var encodedToken = Uri.EscapeDataString(resetToken);
        var link =
            $"{this.options.FrontendBaseUrl.TrimEnd('/')}/reset-password?email={encodedEmail}&token={encodedToken}";
        var subject = "Reset your password";
        var plainText =
            $"A password reset was requested for your account.\n\nReset your password by opening this link:\n{link}\n\nIf you did not request a reset, you can ignore this message.";
        var htmlBody = $"""
            <p>A password reset was requested for your account.</p>
            <p><a href="{link}">Reset your password</a></p>
            <p>If you did not request a reset, you can ignore this message.</p>
            """;

        return emailClient.SendAsync(
            new EmailMessage
            {
                ToAddress = email,
                Subject = subject,
                PlainTextBody = plainText,
                HtmlBody = htmlBody,
            },
            cancellationToken
        );
    }
}
