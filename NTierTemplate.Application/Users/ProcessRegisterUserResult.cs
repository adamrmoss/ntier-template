namespace NTierTemplate.Application.Users;

/// <summary>
/// Outcome of processing a register-user command in the queue worker.
/// </summary>
public class ProcessRegisterUserResult
{
    public bool Succeeded { get; init; }

    public bool IsDuplicateEmail { get; init; }

    public string? ErrorMessage { get; init; }
}
