namespace NTierTemplate.Api.Auth;

/// <summary>
/// Reset password request payload.
/// </summary>
public class ResetPasswordRequest
{
    public required string Email { get; set; }

    public required string Token { get; set; }

    public required string NewPassword { get; set; }
}
