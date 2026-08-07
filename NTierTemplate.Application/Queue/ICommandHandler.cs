namespace NTierTemplate.Application.Queue;

/// <summary>
/// Executes a single domain command type from the queue worker.
/// </summary>
public interface ICommandHandler
{
    /// <summary>
    /// The command payload type this handler supports.
    /// </summary>
    Type CommandType { get; }

    /// <summary>
    /// Execute the deserialized command payload.
    /// </summary>
    /// <param name="command">Command instance matching <see cref="CommandType"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Outcome used to decide retry behavior.</returns>
    Task<CommandHandlerResult> HandleAsync(object command, CancellationToken cancellationToken = default);
}
