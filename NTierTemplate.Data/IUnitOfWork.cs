namespace NTierTemplate.Data;

/// <summary>
/// Coordinate a single database transaction for a use case.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Begin a transaction when one is not already active.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Save pending changes and commit the active transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Roll back the active transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
