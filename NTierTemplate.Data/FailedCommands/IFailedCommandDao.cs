namespace NTierTemplate.Data.FailedCommands;

/// <summary>
/// Persist and query failed queue commands for retry.
/// </summary>
public interface IFailedCommandDao
{
    /// <summary>
    /// Record or update a failed command for later retry.
    /// </summary>
    Task UpsertAsync(FailedCommand failedCommand, CancellationToken cancellationToken = default);

    /// <summary>
    /// Load failed commands that are due for retry.
    /// </summary>
    Task<IReadOnlyList<FailedCommand>> GetDueForRetryAsync(
        DateTime utcNow,
        int take,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Mark a failed command as successfully processed.
    /// </summary>
    Task MarkSucceededAsync(Guid messageId, CancellationToken cancellationToken = default);
}
