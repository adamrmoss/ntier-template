using NTierTemplate.Users;

namespace NTierTemplate.Test.Support;

/// <summary>
/// Factory helpers for domain users in unit tests.
/// </summary>
public static class TestUsers
{
    /// <summary>
    /// Create a sample domain user for assertions.
    /// </summary>
    public static User Create(
        int id = 1,
        string email = "user@example.com",
        string firstName = "Test",
        string lastName = "User",
        string displayName = "Test User",
        string[]? roles = null
    )
    {
        return new User
        {
            Id = id,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            DisplayName = displayName,
            Roles = roles ?? ["User"],
        };
    }
}
