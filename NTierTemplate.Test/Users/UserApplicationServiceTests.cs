using System.Security.Claims;
using Moq;
using NTierTemplate.Application.Users;
using NTierTemplate.Data.Users;
using NTierTemplate.Users;

namespace NTierTemplate.Test.Users;

[TestFixture]
public class UserApplicationServiceTests
{
    private Mock<IUserDao> userDao = null!;
    private Mock<IPrincipalContainer> principalContainer = null!;
    private UserApplicationService service = null!;

    [SetUp]
    public void SetUp()
    {
        this.userDao = new Mock<IUserDao>();
        this.principalContainer = new Mock<IPrincipalContainer>();
        this.service = new UserApplicationService(this.userDao.Object, this.principalContainer.Object);
    }

    [Test]
    public async Task RegisterAsync_ReturnsFailure_WhenEmailAlreadyExists()
    {
        this.userDao
            .Setup(dao => dao.GetByEmailAsync("existing@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(this.CreateUser(email: "existing@example.com"));

        var result = await this.service.RegisterAsync(
            new RegisterUserRequest
            {
                Email = "existing@example.com",
                Password = "password",
            }
        );

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("An account with that email already exists.");
        this.userDao.Verify(
            dao => dao.CreateAsync(It.IsAny<RegisterUserRequest>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
    public async Task RegisterAsync_NormalizesRequestBeforeCreatingUser()
    {
        RegisterUserRequest? capturedRequest = null;

        this.userDao
            .Setup(dao => dao.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        this.userDao
            .Setup(dao => dao.CreateAsync(It.IsAny<RegisterUserRequest>(), It.IsAny<CancellationToken>()))
            .Callback<RegisterUserRequest, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(
                new UserCreateResult
                {
                    Succeeded = true,
                    User = this.CreateUser(),
                }
            );

        await this.service.RegisterAsync(
            new RegisterUserRequest
            {
                Email = "  user@example.com  ",
                Password = "password",
                DisplayName = "  Display Name  ",
                FirstName = "  Ada  ",
                LastName = "  Lovelace  ",
            }
        );

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Email.Should().Be("user@example.com");
        capturedRequest.DisplayName.Should().Be("Display Name");
        capturedRequest.FirstName.Should().Be("Ada");
        capturedRequest.LastName.Should().Be("Lovelace");
    }

    [Test]
    public async Task CreateAdminAsync_ReturnsFailure_WhenEmailAlreadyExists()
    {
        this.userDao
            .Setup(dao => dao.GetByEmailAsync("admin@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(this.CreateUser(email: "admin@example.com"));

        var result = await this.service.CreateAdminAsync("admin@example.com", "password");

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("A user with that email already exists.");
        this.userDao.Verify(
            dao => dao.CreateAdminAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
    public async Task GetCurrentUserAsync_ReturnsNull_WhenPrincipalIsNotAuthenticated()
    {
        this.principalContainer
            .Setup(container => container.Principal)
            .Returns(new ClaimsPrincipal(new ClaimsIdentity()));

        var result = await this.service.GetCurrentUserAsync();

        result.Should().BeNull();
        this.userDao.Verify(
            dao => dao.GetByPrincipalAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
    public async Task GetCurrentUserAsync_ReturnsUser_WhenPrincipalIsAuthenticated()
    {
        var user = this.CreateUser();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "user")], "Bearer"));

        this.principalContainer.Setup(container => container.Principal).Returns(principal);
        this.userDao
            .Setup(dao => dao.GetByPrincipalAsync(principal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await this.service.GetCurrentUserAsync();

        result.Should().BeSameAs(user);
    }

    [Test]
    public async Task ValidatePasswordAsync_DoesNotSignIn_WhenCredentialsAreInvalid()
    {
        this.userDao
            .Setup(dao => dao.ValidatePasswordAsync("user@example.com", "wrong", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await this.service.ValidatePasswordAsync("user@example.com", "wrong");

        result.Should().BeNull();
        this.principalContainer.Verify(container => container.SignIn(It.IsAny<User>()), Times.Never);
    }

    [Test]
    public async Task ValidatePasswordAsync_SignsIn_WhenCredentialsAreValid()
    {
        var user = this.CreateUser();

        this.userDao
            .Setup(dao => dao.ValidatePasswordAsync("user@example.com", "password", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await this.service.ValidatePasswordAsync("user@example.com", "password");

        result.Should().BeSameAs(user);
        this.principalContainer.Verify(container => container.SignIn(user), Times.Once);
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
