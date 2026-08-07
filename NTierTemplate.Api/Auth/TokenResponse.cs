namespace NTierTemplate.Api.Auth;

/// <summary>
/// Token response returned from login, register, and refresh.
/// </summary>
public class TokenResponse
{
    public required string AccessToken { get; set; }

    public required string RefreshToken { get; set; }

    public required DateTime AccessTokenExpiresAt { get; set; }
}
