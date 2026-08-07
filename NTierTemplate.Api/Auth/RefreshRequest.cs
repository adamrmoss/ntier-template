namespace NTierTemplate.Api.Auth;

/// <summary>
/// Refresh request payload.
/// </summary>
public class RefreshRequest
{
    public required string RefreshToken { get; set; }
}
