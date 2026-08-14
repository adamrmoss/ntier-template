using System.Security.Claims;
using Moq;
using NTierTemplate.Application.Auth;
using NTierTemplate.Application.Users;
using NTierTemplate.Data.Users;
using NTierTemplate.Test.Support;
using NTierTemplate.Users;

namespace NTierTemplate.Test.NTierTemplate.Application.Auth;

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
        var user = TestUsers.Create(email: "user@example.com");

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
    public async Task GetCurrentUserAsync_ReturnsUser_WhenPrincipalIsAuthenticated()
    {
        var user = TestUsers.Create();
        var principal = UserPrincipal.Create(user, "Bearer");

        this.principalContainer.Setup(container => container.Principal).Returns(principal);
        this.userDao
            .Setup(dao => dao.GetByPrincipalAsync(principal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await this.service.GetCurrentUserAsync();

        result.Should().BeSameAs(user);
    }

    [Test]
    public async Task IssueRefreshTokenAsync_PersistsHashedToken()
    {
        string? capturedHash = null;

        this.refreshTokenDao
            .Setup(dao => dao.CreateAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<int, string, DateTime, CancellationToken>((_, hash, _, _) => capturedHash = hash)
            .Returns(Task.CompletedTask);

        var rawToken = await this.service.IssueRefreshTokenAsync(7, refreshTokenDays: 14);

        rawToken.Should().NotBeNullOrWhiteSpace();
        capturedHash.Should().NotBeNullOrWhiteSpace();
        capturedHash.Should().NotBe(rawToken);
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

    [Test]
    public async Task RedeemRefreshTokenAsync_ReturnsNull_WhenTokenIsInvalid()
    {
        this.refreshTokenDao
            .Setup(dao => dao.FindValidUserIdByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        var result = await this.service.RedeemRefreshTokenAsync("invalid-token");

        result.Should().BeNull();
        this.refreshTokenDao.Verify(
            dao => dao.RevokeByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
    public async Task RevokeRefreshTokenAsync_DelegatesToRefreshTokenDao()
    {
        await this.service.RevokeRefreshTokenAsync("refresh-token");

        this.refreshTokenDao.Verify(
            dao => dao.RevokeByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
