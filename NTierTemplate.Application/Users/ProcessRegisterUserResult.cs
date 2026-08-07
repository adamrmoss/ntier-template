namespace NTierTemplate.Application.Users;

/// <summary>
/// Outcome of processing a register-user command in the queue worker.
/// </summary>
public class ProcessRegisterUserResult
{
    /// <summary>
    /// Whether registration and confirmation email completed successfully.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Whether the failure was caused by a duplicate email address.
    /// </summary>
    public bool IsDuplicateEmail { get; init; }

    /// <summary>
    /// Failure description when <see cref="Succeeded"/> is false.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
