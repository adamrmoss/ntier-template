namespace NTierTemplate.Api.Auth;

/// <summary>
/// Logout request payload.
/// </summary>
public class LogoutRequest
{
    public string? RefreshToken { get; set; }
}
