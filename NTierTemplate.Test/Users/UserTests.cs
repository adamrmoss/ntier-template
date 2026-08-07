using NTierTemplate.Users;

namespace NTierTemplate.Test.Users;

[TestFixture]
public class UserTests
{
    [Test]
    public void Initials_ReturnsFirstAndLastInitials_WhenBothNamesArePresent()
    {
        var user = this.CreateUser(firstName: "Ada", lastName: "Lovelace", displayName: "Ada Lovelace");

        user.Initials.Should().Be("AL");
    }

    [Test]
    public void Initials_ReturnsFirstInitialOnly_WhenLastNameIsEmpty()
    {
        var user = this.CreateUser(firstName: "Ada", lastName: string.Empty, displayName: "Ada");

        user.Initials.Should().Be("A");
    }

    [Test]
    public void Initials_ReturnsDisplayNameInitial_WhenFirstAndLastNamesAreEmpty()
    {
        var user = this.CreateUser(firstName: string.Empty, lastName: string.Empty, displayName: "Display");

        user.Initials.Should().Be("D");
    }

    [Test]
    public void Initials_ReturnsQuestionMark_WhenNoNameFieldsArePresent()
    {
        var user = this.CreateUser(firstName: string.Empty, lastName: string.Empty, displayName: string.Empty);

        user.Initials.Should().Be("?");
    }

    [Test]
    public void HasRole_ReturnsTrue_WhenRoleMatchesCaseInsensitively()
    {
        var user = this.CreateUser(roles: ["Admin"]);

        user.HasRole("admin").Should().BeTrue();
    }

    [Test]
    public void HasRole_ReturnsFalse_WhenRoleIsMissing()
    {
        var user = this.CreateUser(roles: ["User"]);

        user.HasRole("Admin").Should().BeFalse();
    }

    [Test]
    public void IsAdmin_ReturnsTrue_WhenUserHasAdminRole()
    {
        var user = this.CreateUser(roles: ["Admin", "User"]);

        user.IsAdmin.Should().BeTrue();
    }

    [Test]
    public void IsAdmin_ReturnsFalse_WhenUserDoesNotHaveAdminRole()
    {
        var user = this.CreateUser(roles: ["User"]);

        user.IsAdmin.Should().BeFalse();
    }

    private User CreateUser(
        int id = 1,
        string email = "user@example.com",
        string firstName = "Ada",
        string lastName = "Lovelace",
        string displayName = "Ada Lovelace",
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
