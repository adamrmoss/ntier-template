namespace NTierTemplate.Application.Email;

/// <summary>
/// Outbound SMTP settings for transactional email.
/// </summary>
public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "smtp.office365.com";

    public int Port { get; set; } = 587;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;

    public string FromDisplayName { get; set; } = string.Empty;
}
