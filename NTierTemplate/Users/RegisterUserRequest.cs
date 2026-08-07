namespace NTierTemplate.Users;

/// <summary>
/// Payload for registering a new account user.
/// </summary>
public class RegisterUserRequest
{
    public required string Email { get; set; }

    public required string Password { get; set; }

    public string? DisplayName { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }
}
