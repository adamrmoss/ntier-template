namespace NTierTemplate.Application.Queue;

/// <summary>
/// Strongly typed base class for queue command handlers.
/// </summary>
/// <typeparam name="TCommand">Command payload type from the domain assembly.</typeparam>
public abstract class CommandHandler<TCommand> : ICommandHandler
    where TCommand : class
{
    /// <inheritdoc />
    public Type CommandType => typeof(TCommand);

    /// <inheritdoc />
    public Task<CommandHandlerResult> HandleAsync(object command, CancellationToken cancellationToken = default)
    {
        // Dispatch already deserialized payloads to the strongly typed handler.
        return this.HandleAsync((TCommand)command, cancellationToken);
    }

    /// <summary>
    /// Execute the strongly typed command payload.
    /// </summary>
    /// <param name="command">Command payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Outcome used to decide retry behavior.</returns>
    protected abstract Task<CommandHandlerResult> HandleAsync(
        TCommand command,
        CancellationToken cancellationToken = default
    );
}
