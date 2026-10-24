using Microsoft.EntityFrameworkCore;

namespace NTierTemplate.Data.FailedCommands;

/// <summary>
/// EF Core persistence for failed queue commands.
/// </summary>
public class FailedCommandDao : DaoBase, IFailedCommandDao
{
    /// <summary>
    /// Create failed-command data access bound to the database context.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public FailedCommandDao(NTierTemplateDbContext dbContext)
        : base(dbContext)
    {
    }

    /// <inheritdoc />
    public async Task UpsertAsync(FailedCommand failedCommand, CancellationToken cancellationToken = default)
    {
        // Look up an existing failed-command row by message id.
        var existing = await this.dbContext.FailedCommand
            .SingleOrDefaultAsync(command => command.MessageId == failedCommand.MessageId, cancellationToken);

        if (existing == null)
        {
            // Insert a new failed-command record.
            this.dbContext.FailedCommand.Add(failedCommand);
        }
        else
        {
            // Update retry metadata on the existing record.
            existing.CommandName = failedCommand.CommandName;
            existing.Payload = failedCommand.Payload;
            existing.AttemptCount = failedCommand.AttemptCount;
            existing.MaxAttempts = failedCommand.MaxAttempts;
            existing.LastError = failedCommand.LastError;
            existing.Status = failedCommand.Status;
            existing.FailedAtUtc = failedCommand.FailedAtUtc;
            existing.NextRetryAtUtc = failedCommand.NextRetryAtUtc;
        }

        // Persist the insert or update.
        await this.dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FailedCommand>> GetDueForRetryAsync(
        DateTime utcNow,
        int take,
        CancellationToken cancellationToken = default
    )
    {
        return await this.dbContext.FailedCommand
            .Where(command =>
                command.Status == FailedCommandStatus.PendingRetry
                && command.NextRetryAtUtc != null
                && command.NextRetryAtUtc <= utcNow)
            .OrderBy(command => command.NextRetryAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task MarkSucceededAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        // Load the failed-command row by message id.
        var existing = await this.dbContext.FailedCommand
            .SingleOrDefaultAsync(command => command.MessageId == messageId, cancellationToken);

        if (existing == null)
        {
            return;
        }

        // Mark the command succeeded and clear the next retry time.
        existing.Status = FailedCommandStatus.Succeeded;
        existing.NextRetryAtUtc = null;
        await this.dbContext.SaveChangesAsync(cancellationToken);
    }
}
