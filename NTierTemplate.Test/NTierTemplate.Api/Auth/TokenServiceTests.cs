using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Options;
using Moq;
using NTierTemplate.Api.Auth;
using NTierTemplate.Application.Auth;
using NTierTemplate.Application.Users;
using NTierTemplate.Test.Support;
using NTierTemplate.Users;

namespace NTierTemplate.Test.NTierTemplate.Api.Auth;

[TestFixture]
public class TokenServiceTests
{
    private Mock<IAuthApplicationService> authApplicationService = null!;
    private Mock<IUserApplicationService> userApplicationService = null!;
    private TokenService service = null!;

    [SetUp]
    public void SetUp()
    {
        this.authApplicationService = new Mock<IAuthApplicationService>();
        this.userApplicationService = new Mock<IUserApplicationService>();
        this.service = new TokenService(
            this.authApplicationService.Object,
            this.userApplicationService.Object,
            Options.Create(new JwtOptions
            {
                Issuer = "https://localhost:7240",
                Audience = "http://localhost:8240",
                SigningKey = "01234567890123456789012345678901",
                AccessTokenMinutes = 15,
                RefreshTokenDays = 14,
            })
        );
    }

    [Test]
    public async Task CreateTokenPairAsync_ReturnsSignedJwtWithRoleClaims()
    {
        var user = TestUsers.Create(id: 7, email: "user@example.com", roles: ["User", "Admin"]);

        this.authApplicationService
            .Setup(auth => auth.IssueRefreshTokenAsync(user.Id, 14, It.IsAny<CancellationToken>()))
            .ReturnsAsync("refresh-token");

        var tokens = await this.service.CreateTokenPairAsync(user);

        tokens.AccessToken.Should().NotBeNullOrWhiteSpace();
        tokens.RefreshToken.Should().Be("refresh-token");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokens.AccessToken);

        jwt.Issuer.Should().Be("https://localhost:7240");
        jwt.Audiences.Should().Contain("http://localhost:8240");
        jwt.Claims.Should().Contain(claim => claim.Type == JwtRegisteredClaimNames.Email && claim.Value == "user@example.com");
        jwt.Claims.Should().Contain(claim => claim.Type == System.Security.Claims.ClaimTypes.Role && claim.Value == "Admin");
    }

    [Test]
    public async Task RefreshAsync_ReturnsNull_WhenRefreshTokenIsInvalid()
    {
        this.authApplicationService
            .Setup(auth => auth.RedeemRefreshTokenAsync("invalid", It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        var tokens = await this.service.RefreshAsync("invalid");

        tokens.Should().BeNull();
    }

    [Test]
    public async Task RefreshAsync_ReturnsNewTokenPair_WhenRefreshTokenIsValid()
    {
        var user = TestUsers.Create(id: 9);

        this.authApplicationService
            .Setup(auth => auth.RedeemRefreshTokenAsync("refresh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user.Id);
        this.userApplicationService
            .Setup(users => users.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        this.authApplicationService
            .Setup(auth => auth.IssueRefreshTokenAsync(user.Id, 14, It.IsAny<CancellationToken>()))
            .ReturnsAsync("rotated-refresh-token");

        var tokens = await this.service.RefreshAsync("refresh-token");

        tokens.Should().NotBeNull();
        tokens!.RefreshToken.Should().Be("rotated-refresh-token");
    }

    [Test]
    public async Task RevokeRefreshTokenAsync_DelegatesToAuthApplicationService()
    {
        await this.service.RevokeRefreshTokenAsync("refresh-token");

        this.authApplicationService.Verify(
            auth => auth.RevokeRefreshTokenAsync("refresh-token", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
