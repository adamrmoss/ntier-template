namespace NTierTemplate.Api.Auth;

/// <summary>
/// Login request payload.
/// </summary>
public class LoginRequest
{
    public required string Email { get; set; }

    public required string Password { get; set; }
}
