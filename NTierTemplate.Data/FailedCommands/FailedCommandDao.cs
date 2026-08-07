using Microsoft.EntityFrameworkCore;

namespace NTierTemplate.Data.FailedCommands;

/// <summary>
/// EF Core persistence for failed queue commands.
/// </summary>
public class FailedCommandDao(NTierTemplateDbContext dbContext) : DaoBase, IFailedCommandDao
{
    /// <inheritdoc />
    public async Task UpsertAsync(FailedCommand failedCommand, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.FailedCommand
            .SingleOrDefaultAsync(command => command.MessageId == failedCommand.MessageId, cancellationToken);

        if (existing == null)
        {
            dbContext.FailedCommand.Add(failedCommand);
        }
        else
        {
            existing.CommandName = failedCommand.CommandName;
            existing.Payload = failedCommand.Payload;
            existing.AttemptCount = failedCommand.AttemptCount;
            existing.MaxAttempts = failedCommand.MaxAttempts;
            existing.LastError = failedCommand.LastError;
            existing.Status = failedCommand.Status;
            existing.FailedAtUtc = failedCommand.FailedAtUtc;
            existing.NextRetryAtUtc = failedCommand.NextRetryAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FailedCommand>> GetDueForRetryAsync(
        DateTime utcNow,
        int take,
        CancellationToken cancellationToken = default
    )
    {
        return await dbContext.FailedCommand
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
        var existing = await dbContext.FailedCommand
            .SingleOrDefaultAsync(command => command.MessageId == messageId, cancellationToken);

        if (existing == null)
        {
            return;
        }

        existing.Status = FailedCommandStatus.Succeeded;
        existing.NextRetryAtUtc = null;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
