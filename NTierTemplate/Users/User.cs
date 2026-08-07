namespace NTierTemplate.Users;

/// <summary>
/// A signed-in NTierTemplate account user.
/// </summary>
public class User
{
    public required int Id { get; set; }

    public required string Email { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public required string DisplayName { get; set; }

    public required string[] Roles { get; set; } = ["User"];

    /// <summary>
    /// Avatar initials derived from the user's name.
    /// </summary>
    public string Initials
    {
        get
        {
            string firstInitial = this.FirstName.Length > 0 ? this.FirstName[0].ToString() : string.Empty;
            string lastInitial = this.LastName.Length > 0 ? this.LastName[0].ToString() : string.Empty;

            if (firstInitial.Length > 0 && lastInitial.Length > 0)
            {
                return string.Concat(firstInitial, lastInitial).ToUpperInvariant();
            }

            if (firstInitial.Length > 0)
            {
                return firstInitial.ToUpperInvariant();
            }

            return this.DisplayName.Length > 0
                ? this.DisplayName[0].ToString().ToUpperInvariant()
                : "?";
        }
    }

    /// <summary>
    /// Determine whether the user has a specific role.
    /// </summary>
    ///
    /// <param name="role">Role to check.</param>
    public bool HasRole(string role)
    {
        return this.Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Whether the user is an administrator.
    /// </summary>
    public bool IsAdmin
    {
        get
        {
            return this.HasRole("Admin");
        }
    }
}
