using System.Security.Claims;
using Moq;
using NTierTemplate.Application.Auth;
using NTierTemplate.Data.Users;
using NTierTemplate.Users;

namespace NTierTemplate.Test.Auth;

[TestFixture]
public class AuthApplicationServiceTests
{
    private Mock<IUserDao> userDao = null!;
    private Mock<IRefreshTokenDao> refreshTokenDao = null!;
    private Mock<IPrincipalContainer> principalContainer = null!;
    private AuthApplicationService service = null!;

    [SetUp]
    public void SetUp()
    {
        this.userDao = new Mock<IUserDao>();
        this.refreshTokenDao = new Mock<IRefreshTokenDao>();
        this.principalContainer = new Mock<IPrincipalContainer>();
        this.service = new AuthApplicationService(
            this.userDao.Object,
            this.refreshTokenDao.Object,
            this.principalContainer.Object
        );
    }

    [Test]
    public async Task ValidatePasswordAsync_SignsInUser_WhenCredentialsAreValid()
    {
        var user = new User
        {
            Id = 1,
            Email = "user@example.com",
            FirstName = "Test",
            LastName = "User",
        };

        this.userDao
            .Setup(dao => dao.ValidatePasswordAsync("user@example.com", "Password1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await this.service.ValidatePasswordAsync("user@example.com", "Password1");

        result.Should().BeSameAs(user);
        this.principalContainer.Verify(container => container.SignIn(user), Times.Once);
    }

    [Test]
    public async Task ValidatePasswordAsync_DoesNotSignInUser_WhenCredentialsAreInvalid()
    {
        this.userDao
            .Setup(dao => dao.ValidatePasswordAsync("user@example.com", "wrong", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await this.service.ValidatePasswordAsync("user@example.com", "wrong");

        result.Should().BeNull();
        this.principalContainer.Verify(container => container.SignIn(It.IsAny<User>()), Times.Never);
    }

    [Test]
    public async Task GetCurrentUserAsync_ReturnsNull_WhenPrincipalIsNotAuthenticated()
    {
        this.principalContainer
            .Setup(container => container.Principal)
            .Returns(new ClaimsPrincipal(new ClaimsIdentity()));

        var result = await this.service.GetCurrentUserAsync();

        result.Should().BeNull();
    }

    [Test]
    public async Task RedeemRefreshTokenAsync_ReturnsUserId_WhenTokenIsValid()
    {
        this.refreshTokenDao
            .Setup(dao => dao.FindValidUserIdByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var result = await this.service.RedeemRefreshTokenAsync("refresh-token");

        result.Should().Be(42);
        this.refreshTokenDao.Verify(
            dao => dao.RevokeByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
