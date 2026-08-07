namespace NTierTemplate.Api.Auth;

/// <summary>
/// Registration request payload.
/// </summary>
public class RegisterRequest
{
    public required string Email { get; set; }

    public required string Password { get; set; }

    public string? DisplayName { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }
}
