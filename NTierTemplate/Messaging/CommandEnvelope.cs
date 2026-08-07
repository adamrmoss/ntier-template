namespace NTierTemplate.Messaging;

/// <summary>
/// Queue message wrapper for a domain command payload.
/// </summary>
public class CommandEnvelope
{
    public Guid MessageId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// CLR assembly-qualified type name of the command payload, with full-name fallback for older messages.
    /// </summary>
    public string CommandName { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public int Attempt { get; set; } = 1;

    public int MaxAttempts { get; set; } = 3;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
