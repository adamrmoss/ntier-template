namespace NTierTemplate.Api.Auth;

/// <summary>
/// Forgot password request payload.
/// </summary>
public class ForgotPasswordRequest
{
    public required string Email { get; set; }
}
