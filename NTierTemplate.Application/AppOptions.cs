namespace NTierTemplate.Application;

/// <summary>
/// Application-wide settings for link generation and frontend integration.
/// </summary>
public class AppOptions
{
    public const string SectionName = "App";

    public string FrontendBaseUrl { get; set; } = "http://localhost:8240";
}
