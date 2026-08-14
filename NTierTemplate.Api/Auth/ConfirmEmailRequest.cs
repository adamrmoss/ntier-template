namespace NTierTemplate.Api.Auth;

/// <summary>
/// Email confirmation request payload.
/// </summary>
public class ConfirmEmailRequest
{
    public required int UserId { get; set; }

    public required string Token { get; set; }
}
