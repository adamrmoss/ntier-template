namespace NTierTemplate.Application.Queue;

/// <summary>
/// Outcome of executing a queue command handler.
/// </summary>
public sealed class CommandHandlerResult
{
    /// <summary>
    /// Whether the command completed successfully.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Whether the failure should not be retried.
    /// </summary>
    public bool IsPermanentFailure { get; init; }

    /// <summary>
    /// Failure description when <see cref="Succeeded"/> is false.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Create a successful handler outcome.
    /// </summary>
    /// <returns>A result with <see cref="Succeeded"/> set to true.</returns>
    public static CommandHandlerResult Success()
    {
        return new CommandHandlerResult
        {
            Succeeded = true,
        };
    }

    /// <summary>
    /// Create a permanent failure outcome that should not be retried.
    /// </summary>
    /// <param name="errorMessage">Failure description.</param>
    /// <returns>A result with <see cref="IsPermanentFailure"/> set to true.</returns>
    public static CommandHandlerResult PermanentFailure(string errorMessage)
    {
        return new CommandHandlerResult
        {
            Succeeded = false,
            IsPermanentFailure = true,
            ErrorMessage = errorMessage,
        };
    }

    /// <summary>
    /// Create a transient failure outcome that may be retried.
    /// </summary>
    /// <param name="errorMessage">Failure description.</param>
    /// <returns>A result with <see cref="IsPermanentFailure"/> set to false.</returns>
    public static CommandHandlerResult TransientFailure(string errorMessage)
    {
        return new CommandHandlerResult
        {
            Succeeded = false,
            IsPermanentFailure = false,
            ErrorMessage = errorMessage,
        };
    }
}
