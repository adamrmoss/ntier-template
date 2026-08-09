using NTierTemplate.Application.Queue;
using NTierTemplate.Application.Users;
using NTierTemplate.Users;

namespace NTierTemplate.Queue.Users;

/// <summary>
/// Processes register-user commands from the queue worker.
/// </summary>
public sealed class RegisterUserCommandHandler(IUserApplicationService userApplicationService)
    : CommandHandler<RegisterUserCommand>
{
    /// <inheritdoc />
    protected override async Task<CommandHandlerResult> HandleAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken = default
    )
    {
        // Delegate to the application service, which owns the transaction boundary.
        var result = await userApplicationService.ProcessRegisterUserAsync(
            command.ToRequest(),
            cancellationToken
        );

        if (result.Succeeded)
        {
            return CommandHandlerResult.Success();
        }

        // Map duplicate email to a permanent failure; everything else may retry.
        return result.IsDuplicateEmail
            ? CommandHandlerResult.PermanentFailure(result.ErrorMessage ?? "Duplicate email.")
            : CommandHandlerResult.TransientFailure(result.ErrorMessage ?? "Registration failed.");
    }
}
