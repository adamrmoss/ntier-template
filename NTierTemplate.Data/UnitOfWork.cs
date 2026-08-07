using Microsoft.EntityFrameworkCore.Storage;

namespace NTierTemplate.Data;

/// <summary>
/// EF Core transaction boundary for application use cases.
/// </summary>
public sealed class UnitOfWork(NTierTemplateDbContext dbContext) : IUnitOfWork, IAsyncDisposable
{
    private IDbContextTransaction? transaction;

    /// <inheritdoc />
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        // Reuse an already-open transaction.
        if (this.transaction != null)
        {
            return;
        }

        // Begin a new database transaction.
        this.transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (this.transaction == null)
        {
            // Persist pending changes without an explicit transaction.
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        // Commit pending changes and the open transaction.
        await dbContext.SaveChangesAsync(cancellationToken);
        await this.transaction.CommitAsync(cancellationToken);
        await this.DisposeTransactionAsync();
    }

    /// <inheritdoc />
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (this.transaction == null)
        {
            return;
        }

        // Roll back the open transaction and release it.
        await this.transaction.RollbackAsync(cancellationToken);
        await this.DisposeTransactionAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await this.DisposeTransactionAsync();
    }

    private async Task DisposeTransactionAsync()
    {
        if (this.transaction == null)
        {
            return;
        }

        // Dispose the transaction and clear the field.
        await this.transaction.DisposeAsync();
        this.transaction = null;
    }
}
