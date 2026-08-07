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
        if (this.transaction != null)
        {
            return;
        }

        this.transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (this.transaction == null)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

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

        await this.transaction.DisposeAsync();
        this.transaction = null;
    }
}
