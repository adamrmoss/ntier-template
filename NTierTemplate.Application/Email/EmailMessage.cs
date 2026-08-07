namespace NTierTemplate.Application.Email;

/// <summary>
/// Outbound email payload.
/// </summary>
public class EmailMessage
{
    public required string ToAddress { get; set; }

    public required string Subject { get; set; }

    public required string PlainTextBody { get; set; }

    public string? HtmlBody { get; set; }
}
