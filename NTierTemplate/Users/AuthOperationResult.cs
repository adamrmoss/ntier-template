namespace NTierTemplate.Users;

/// <summary>
/// Result of an authentication-related persistence operation.
/// </summary>
public class AuthOperationResult
{
    public required bool Succeeded { get; set; }

    public User? User { get; set; }

    public string[] Errors { get; set; } = [];
}
