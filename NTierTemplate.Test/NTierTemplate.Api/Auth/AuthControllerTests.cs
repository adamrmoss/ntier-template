using Microsoft.AspNetCore.Mvc;
using Moq;
using NTierTemplate.Api.Auth;
using NTierTemplate.Application.Auth;
using NTierTemplate.Application.Users;
using NTierTemplate.Test.Support;
using NTierTemplate.Users;

namespace NTierTemplate.Test.NTierTemplate.Api.Auth;

[TestFixture]
public class AuthControllerTests
{
    private Mock<ITokenService> tokenService = null!;
    private Mock<IAuthApplicationService> authApplicationService = null!;
    private Mock<IUserApplicationService> userApplicationService = null!;
    private AuthController controller = null!;

    [SetUp]
    public void SetUp()
    {
        this.tokenService = new Mock<ITokenService>();
        this.authApplicationService = new Mock<IAuthApplicationService>();
        this.userApplicationService = new Mock<IUserApplicationService>();
        this.controller = new AuthController(
            this.tokenService.Object,
            this.authApplicationService.Object,
            this.userApplicationService.Object,
            TestJsonOptionsMonitor.CreateCamelCaseMonitor()
        );
    }

    [Test]
    public async Task Register_ReturnsConflict_WhenEmailAlreadyExists()
    {
        this.userApplicationService
            .Setup(service => service.GetByEmailAsync("existing@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUsers.Create(email: "existing@example.com"));

        var result = await this.controller.Register(
            new RegisterRequest
            {
                Email = "existing@example.com",
                Password = "Password1",
            },
            CancellationToken.None
        );

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Test]
    public async Task Register_ReturnsAccepted_WhenRegistrationIsQueued()
    {
        this.userApplicationService
            .Setup(service => service.GetByEmailAsync("new@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        this.userApplicationService
            .Setup(service => service.EnqueueRegisterUserAsync(It.IsAny<RegisterUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        var result = await this.controller.Register(
            new RegisterRequest
            {
                Email = "new@example.com",
                Password = "Password1",
            },
            CancellationToken.None
        );

        result.Should().BeAssignableTo<ObjectResult>();
        ((ObjectResult)result).StatusCode.Should().Be(202);
    }

    [Test]
    public async Task Login_ReturnsUnauthorized_WhenEmailIsUnconfirmed()
    {
        this.authApplicationService
            .Setup(auth => auth.ValidatePasswordAsync("user@example.com", "Password1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        this.authApplicationService
            .Setup(auth => auth.CheckPasswordAsync("user@example.com", "Password1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        this.authApplicationService
            .Setup(auth => auth.IsEmailConfirmedAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await this.controller.Login(
            new LoginRequest
            {
                Email = "user@example.com",
                Password = "Password1",
            },
            CancellationToken.None
        );

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Test]
    public async Task Login_ReturnsOkJson_WhenCredentialsAreValid()
    {
        var user = TestUsers.Create(email: "user@example.com");
        var tokenResponse = new TokenResponse
        {
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15),
        };

        this.authApplicationService
            .Setup(auth => auth.ValidatePasswordAsync("user@example.com", "Password1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        this.tokenService
            .Setup(tokens => tokens.CreateTokenPairAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenResponse);

        var result = await this.controller.Login(
            new LoginRequest
            {
                Email = "user@example.com",
                Password = "Password1",
            },
            CancellationToken.None
        );

        result.Should().BeOfType<OkObjectResult>();
        ((OkObjectResult)result).Value.Should().BeOfType<string>();
        ((string)((OkObjectResult)result).Value!).Should().Contain("accessToken");
    }

    [Test]
    public async Task Refresh_ReturnsUnauthorized_WhenRefreshTokenIsInvalid()
    {
        this.tokenService
            .Setup(tokens => tokens.RefreshAsync("invalid", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TokenResponse?)null);

        var result = await this.controller.Refresh(
            new RefreshRequest
            {
                RefreshToken = "invalid",
            },
            CancellationToken.None
        );

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }
}
