namespace NTierTemplate.Users;

/// <summary>
/// Result of creating an account user.
/// </summary>
public class UserCreateResult
{
    public required bool Succeeded { get; set; }

    public User? User { get; set; }

    public string[] Errors { get; set; } = [];
}
