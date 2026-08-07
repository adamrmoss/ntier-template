using NTierTemplate.Application.Email;
using NTierTemplate.Application.Users;
using NTierTemplate.Users;

namespace NTierTemplate.Application.Auth;

/// <summary>
/// Authentication use cases shared across entry-point hosts.
/// </summary>
public class AuthApplicationService(
    IUserApplicationService userApplicationService,
    IAuthEmailService authEmailService
)
    : IAuthApplicationService
{
    /// <inheritdoc />
    public async Task<RegistrationResult> RegisterAndSendConfirmationAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var createResult = await userApplicationService.RegisterAsync(request, cancellationToken);

        if (!createResult.Succeeded || createResult.User == null)
        {
            return new RegistrationResult
            {
                CreateResult = createResult,
                ConfirmationEmailSent = false,
            };
        }

        var confirmationToken = await userApplicationService.GenerateEmailConfirmationTokenAsync(
            createResult.User.Id,
            cancellationToken
        );

        if (confirmationToken == null)
        {
            return new RegistrationResult
            {
                CreateResult = createResult,
                ConfirmationEmailSent = false,
            };
        }

        await authEmailService.SendEmailConfirmationAsync(
            createResult.User,
            confirmationToken,
            cancellationToken
        );

        return new RegistrationResult
        {
            CreateResult = createResult,
            ConfirmationEmailSent = true,
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
            await authEmailService.SendEmailConfirmationAsync(user, token, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task SendPasswordResetEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var token = await userApplicationService.GeneratePasswordResetTokenAsync(email, cancellationToken);

        if (token != null)
        {
            await authEmailService.SendPasswordResetAsync(email, token, cancellationToken);
        }
    }
}
