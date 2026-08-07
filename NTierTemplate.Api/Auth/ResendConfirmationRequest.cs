namespace NTierTemplate.Api.Auth;

/// <summary>
/// Resend email confirmation request payload.
/// </summary>
public class ResendConfirmationRequest
{
    public required string Email { get; set; }
}
