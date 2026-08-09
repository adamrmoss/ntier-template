namespace NTierTemplate.Users;

/// <summary>
/// Command payload to register a standard user account asynchronously.
/// </summary>
public class RegisterUserCommand
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Map this command to the registration request used by application services.
    /// </summary>
    public RegisterUserRequest ToRequest()
    {
        return new RegisterUserRequest
        {
            Email = this.Email,
            Password = this.Password,
            DisplayName = this.DisplayName,
            FirstName = this.FirstName,
            LastName = this.LastName,
        };
    }
}
