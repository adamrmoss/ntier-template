namespace NTierTemplate.Api.Auth;

/// <summary>
/// JWT bearer token settings.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "https://localhost:7240";

    public string Audience { get; set; } = "http://localhost:8240";

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 14;
}
