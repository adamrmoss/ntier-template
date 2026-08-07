using NTierTemplate.Users;

namespace NTierTemplate.Application.Auth;

/// <summary>
/// Outcome of registering an account and sending a confirmation email.
/// </summary>
public class RegistrationResult
{
    public required UserCreateResult CreateResult { get; init; }

    public bool ConfirmationEmailSent { get; init; }
}
