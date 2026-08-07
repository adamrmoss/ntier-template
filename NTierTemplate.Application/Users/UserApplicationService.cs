using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NTierTemplate.Application.Email;
using NTierTemplate.Application.Queue;
using NTierTemplate.Data;
using NTierTemplate.Data.Users;
using NTierTemplate.Users;

namespace NTierTemplate.Application.Users;

/// <summary>
/// User account maintenance, registration, and transactional email use cases.
/// </summary>
public class UserApplicationService(
    IUserDao userDao,
    IQueueApplicationService queueApplicationService,
    IEmailClient emailClient,
    IUnitOfWork unitOfWork,
    IOptions<AppOptions> appOptions,
    ILogger<UserApplicationService> logger
)
    : IUserApplicationService
{
    private readonly AppOptions options = appOptions.Value;

    /// <inheritdoc />
    public Task EnsureDefaultRolesExistAsync(CancellationToken cancellationToken = default)
    {
        return userDao.EnsureDefaultRolesExistAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<Guid> EnqueueRegisterUserAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default
    )
    {
        // Build the queue command from the registration request.
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
        try
        {
            // Run registration inside a transaction boundary.
            await unitOfWork.BeginTransactionAsync(cancellationToken);

            // Create the account through Identity.
            var createResult = await this.RegisterAsync(request, cancellationToken);

            if (!createResult.Succeeded || createResult.User == null)
            {
                // Classify duplicate-email failures as permanent.
                var isDuplicateEmail = createResult.Errors.Any(error =>
                    error.Contains("already exists", StringComparison.OrdinalIgnoreCase));

                await unitOfWork.RollbackAsync(cancellationToken);

                return new ProcessRegisterUserResult
                {
                    Succeeded = false,
                    IsDuplicateEmail = isDuplicateEmail,
                    ErrorMessage = createResult.Errors.FirstOrDefault() ?? "Registration failed.",
                };
            }

            // Generate an email confirmation token for the new account.
            var confirmationToken = await userDao.GenerateEmailConfirmationTokenAsync(
                createResult.User.Id,
                cancellationToken
            );

            if (confirmationToken == null)
            {
                // Roll back when Identity cannot produce a confirmation token.
                await unitOfWork.RollbackAsync(cancellationToken);

                return new ProcessRegisterUserResult
                {
                    Succeeded = false,
                    ErrorMessage = "Could not generate email confirmation token.",
                };
            }

            // Send the confirmation email to the new user.
            await this.SendEmailConfirmationAsync(createResult.User, confirmationToken, cancellationToken);

            // Commit the registration and confirmation email work.
            await unitOfWork.CommitAsync(cancellationToken);

            return new ProcessRegisterUserResult
            {
                Succeeded = true,
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Register user command failed.");

            try
            {
                // Best-effort rollback after an unexpected error.
                await unitOfWork.RollbackAsync(cancellationToken);
            }
            catch (Exception rollbackException)
            {
                logger.LogWarning(rollbackException, "Rollback failed after register user error.");
            }

            return new ProcessRegisterUserResult
            {
                Succeeded = false,
                ErrorMessage = exception.Message,
            };
        }
    }

    /// <inheritdoc />
    public async Task<UserCreateResult> RegisterAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default
    )
    {
        // Reject registration when the email is already taken.
        var existingUser = await userDao.GetByEmailAsync(request.Email, cancellationToken);

        if (existingUser != null)
        {
            return new UserCreateResult
            {
                Succeeded = false,
                Errors = ["An account with that email already exists."],
            };
        }

        // Normalize request fields before persistence.
        var normalizedRequest = new RegisterUserRequest
        {
            Email = request.Email.Trim(),
            Password = request.Password,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim(),
            FirstName = string.IsNullOrWhiteSpace(request.FirstName) ? string.Empty : request.FirstName.Trim(),
            LastName = string.IsNullOrWhiteSpace(request.LastName) ? string.Empty : request.LastName.Trim(),
        };

        // Persist the new account through the user DAO.
        return await userDao.CreateAsync(normalizedRequest, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserCreateResult> CreateAdminAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default
    )
    {
        // Reject creation when the email is already taken.
        var existingUser = await userDao.GetByEmailAsync(email, cancellationToken);

        if (existingUser != null)
        {
            return new UserCreateResult
            {
                Succeeded = false,
                Errors = ["A user with that email already exists."],
            };
        }

        // Create the administrator account through the user DAO.
        return await userDao.CreateAdminAsync(email, password, cancellationToken);
    }

    /// <inheritdoc />
    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return userDao.GetByIdAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return userDao.GetByEmailAsync(email, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ResendConfirmationEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        // Load the account and skip confirmed or missing users.
        var user = await userDao.GetByEmailAsync(email, cancellationToken);

        if (user == null || await userDao.IsEmailConfirmedAsync(email, cancellationToken))
        {
            return;
        }

        // Generate a fresh confirmation token and send the email.
        var token = await userDao.GenerateEmailConfirmationTokenAsync(user.Id, cancellationToken);

        if (token != null)
        {
            await this.SendEmailConfirmationAsync(user, token, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task SendPasswordResetEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        // Generate a password reset token when the account exists.
        var token = await userDao.GeneratePasswordResetTokenAsync(email, cancellationToken);

        if (token != null)
        {
            await this.SendPasswordResetAsync(email, token, cancellationToken);
        }
    }

    /// <inheritdoc />
    public Task<AuthOperationResult> ConfirmEmailAsync(
        int userId,
        string token,
        CancellationToken cancellationToken = default
    )
    {
        return userDao.ConfirmEmailAsync(userId, token, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AuthOperationResult> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default
    )
    {
        return userDao.ResetPasswordAsync(email, token, newPassword, cancellationToken);
    }

    private Task SendEmailConfirmationAsync(
        User user,
        string confirmationToken,
        CancellationToken cancellationToken
    )
    {
        // Build the frontend confirmation link with an encoded token.
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

        // Send the confirmation email.
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
        // Build the frontend reset link with encoded email and token.
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

        // Send the password reset email.
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
