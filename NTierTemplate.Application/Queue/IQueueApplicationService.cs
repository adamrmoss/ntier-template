using NTierTemplate.Messaging;

namespace NTierTemplate.Application.Queue;

/// <summary>
/// Entry point for enqueueing and processing queue commands.
/// </summary>
public interface IQueueApplicationService
{
    /// <summary>
    /// Publish a command to RabbitMQ and return the message id.
    /// </summary>
    /// <typeparam name="TCommand">Command type serialized in the envelope.</typeparam>
    /// <param name="command">Command payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Guid> EnqueueAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : class;

    /// <summary>
    /// Execute a queued command and record retry state when it fails.
    /// </summary>
    /// <param name="envelope">Queue envelope.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ProcessAsync(CommandEnvelope envelope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Republish failed commands that are due for retry.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RetryDueAsync(CancellationToken cancellationToken = default);
}
