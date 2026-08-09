using NTierTemplate.Users;
using NTierTemplate.Test.Support;

namespace NTierTemplate.Test.NTierTemplate.Users;

[TestFixture]
public class UserTests
{
    [Test]
    public void Initials_ReturnsFirstAndLastInitials_WhenBothNamesArePresent()
    {
        var user = TestUsers.Create(firstName: "Ada", lastName: "Lovelace", displayName: "Ada Lovelace");

        user.Initials.Should().Be("AL");
    }

    [Test]
    public void Initials_ReturnsFirstInitialOnly_WhenLastNameIsEmpty()
    {
        var user = TestUsers.Create(firstName: "Ada", lastName: string.Empty, displayName: "Ada");

        user.Initials.Should().Be("A");
    }

    [Test]
    public void Initials_ReturnsDisplayNameInitial_WhenFirstAndLastNamesAreEmpty()
    {
        var user = TestUsers.Create(firstName: string.Empty, lastName: string.Empty, displayName: "Display");

        user.Initials.Should().Be("D");
    }

    [Test]
    public void Initials_ReturnsQuestionMark_WhenNoNameFieldsArePresent()
    {
        var user = TestUsers.Create(firstName: string.Empty, lastName: string.Empty, displayName: string.Empty);

        user.Initials.Should().Be("?");
    }

    [Test]
    public void HasRole_ReturnsTrue_WhenRoleMatchesCaseInsensitively()
    {
        var user = TestUsers.Create(roles: ["Admin"]);

        user.HasRole("admin").Should().BeTrue();
    }

    [Test]
    public void HasRole_ReturnsFalse_WhenRoleIsMissing()
    {
        var user = TestUsers.Create(roles: ["User"]);

        user.HasRole("Admin").Should().BeFalse();
    }

    [Test]
    public void IsAdmin_ReturnsTrue_WhenUserHasAdminRole()
    {
        var user = TestUsers.Create(roles: ["Admin", "User"]);

        user.IsAdmin.Should().BeTrue();
    }

    [Test]
    public void IsAdmin_ReturnsFalse_WhenUserDoesNotHaveAdminRole()
    {
        var user = TestUsers.Create(roles: ["User"]);

        user.IsAdmin.Should().BeFalse();
    }
}
