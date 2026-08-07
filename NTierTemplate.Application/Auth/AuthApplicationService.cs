using Microsoft.Extensions.Options;
using NTierTemplate.Application.Email;
using NTierTemplate.Application.Queue;
using NTierTemplate.Application.Users;
using NTierTemplate.Users;

namespace NTierTemplate.Application.Auth;

/// <summary>
/// Authentication use cases shared across entry-point hosts.
/// </summary>
public class AuthApplicationService(
    IUserApplicationService userApplicationService,
    IQueueApplicationService queueApplicationService,
    IEmailClient emailClient,
    IOptions<AppOptions> appOptions
)
    : IAuthApplicationService
{
    private readonly AppOptions options = appOptions.Value;

    /// <inheritdoc />
    public Task<Guid> EnqueueRegisterUserAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var command = new RegisterUserCommand
        {
            Email = request.Email,
            Password = request.Password,
            DisplayName = request.DisplayName,
            FirstName = request.FirstName ?? string.Empty,
            LastName = request.LastName ?? string.Empty,
        };

        return queueApplicationService.EnqueueAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProcessRegisterUserResult> ProcessRegisterUserAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var createResult = await userApplicationService.RegisterAsync(request, cancellationToken);

        if (!createResult.Succeeded || createResult.User == null)
        {
            var isDuplicateEmail = createResult.Errors.Any(error =>
                error.Contains("already exists", StringComparison.OrdinalIgnoreCase));

            return new ProcessRegisterUserResult
            {
                Succeeded = false,
                IsDuplicateEmail = isDuplicateEmail,
                ErrorMessage = createResult.Errors.FirstOrDefault() ?? "Registration failed.",
            };
        }

        var confirmationToken = await userApplicationService.GenerateEmailConfirmationTokenAsync(
            createResult.User.Id,
            cancellationToken
        );

        if (confirmationToken == null)
        {
            return new ProcessRegisterUserResult
            {
                Succeeded = false,
                ErrorMessage = "Could not generate email confirmation token.",
            };
        }

        await this.SendEmailConfirmationAsync(createResult.User, confirmationToken, cancellationToken);

        return new ProcessRegisterUserResult
        {
            Succeeded = true,
        };
    }

    /// <inheritdoc />
    public async Task ResendConfirmationEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await userApplicationService.GetByEmailAsync(email, cancellationToken);

        if (user == null || await userApplicationService.IsEmailConfirmedAsync(email, cancellationToken))
        {
            return;
        }

        var token = await userApplicationService.GenerateEmailConfirmationTokenAsync(
            user.Id,
            cancellationToken
        );

        if (token != null)
        {
            await this.SendEmailConfirmationAsync(user, token, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task SendPasswordResetEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var token = await userApplicationService.GeneratePasswordResetTokenAsync(email, cancellationToken);

        if (token != null)
        {
            await this.SendPasswordResetAsync(email, token, cancellationToken);
        }
    }

    private Task SendEmailConfirmationAsync(
        User user,
        string confirmationToken,
        CancellationToken cancellationToken
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

    private Task SendPasswordResetAsync(
        string email,
        string resetToken,
        CancellationToken cancellationToken
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
