namespace NTierTemplate.Data.FailedCommands;

/// <summary>
/// Retry state for a failed queue command. Stored in the database as the member name string.
/// </summary>
public enum FailedCommandStatus
{
    PendingRetry = 0,
    Exhausted = 1,
    Succeeded = 2,
}
