using NTierTemplate.Data.FailedCommands;
using NTierTemplate.Data.Users;
using NTierTemplate.Test.Support;
using NTierTemplate.Users;

namespace NTierTemplate.Test.NTierTemplate.Data.Users;

[TestFixture]
public class UserDaoTests : InMemoryDataTestBase
{
    private UserDao userDao = null!;

    [SetUp]
    public void SetUpUserDao()
    {
        this.userDao = new UserDao(this.DbContext, this.UserManager, this.RoleManager);
    }

    [Test]
    public async Task CreateAsync_AssignsDefaultUserRole()
    {
        var result = await this.userDao.CreateAsync(
            new RegisterUserRequest
            {
                Email = "new@example.com",
                Password = "Password1",
                FirstName = "Ada",
                LastName = "Lovelace",
            }
        );

        result.Succeeded.Should().BeTrue();
        result.User!.Roles.Should().Contain("User");
    }

    [Test]
    public async Task ValidatePasswordAsync_ReturnsNull_WhenEmailIsNotConfirmed()
    {
        await this.userDao.CreateAsync(
            new RegisterUserRequest
            {
                Email = "pending@example.com",
                Password = "Password1",
            }
        );

        var validatedUser = await this.userDao.ValidatePasswordAsync("pending@example.com", "Password1");

        validatedUser.Should().BeNull();
    }

    [Test]
    public async Task ValidatePasswordAsync_ReturnsUser_WhenEmailIsConfirmed()
    {
        await this.SeedConfirmedUserAsync("confirmed@example.com");

        var validatedUser = await this.userDao.ValidatePasswordAsync("confirmed@example.com", "Password1");

        validatedUser.Should().NotBeNull();
        validatedUser!.Email.Should().Be("confirmed@example.com");
    }

    [Test]
    public async Task GetByEmailAsync_UsesEmailAsDisplayName_WhenDisplayNameIsMissing()
    {
        await this.userDao.CreateAsync(
            new RegisterUserRequest
            {
                Email = "plain@example.com",
                Password = "Password1",
            }
        );

        var user = await this.userDao.GetByEmailAsync("plain@example.com");

        user.Should().NotBeNull();
        user!.DisplayName.Should().Be("plain@example.com");
    }

    [Test]
    public async Task CreateAdminAsync_AssignsAdminAndUserRoles()
    {
        var result = await this.userDao.CreateAdminAsync("admin@example.com", "Password1");

        result.Succeeded.Should().BeTrue();
        result.User!.Roles.Should().Contain("Admin");
        result.User.Roles.Should().Contain("User");
    }
}
