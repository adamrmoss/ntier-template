using System.Security.Cryptography;
using System.Text;
using NTierTemplate.Data.Users;
using NTierTemplate.Test.Support;

namespace NTierTemplate.Test.NTierTemplate.Data.Users;

[TestFixture]
public class RefreshTokenDaoTests : InMemoryDataTestBase
{
    private RefreshTokenDao refreshTokenDao = null!;

    [SetUp]
    public void SetUpRefreshTokenDao()
    {
        this.refreshTokenDao = new RefreshTokenDao(this.DbContext);
    }

    [Test]
    public async Task FindValidUserIdByTokenHashAsync_ReturnsUserId_WhenTokenIsActive()
    {
        var user = await this.SeedConfirmedUserAsync();
        var tokenHash = HashToken("refresh-token");

        await this.refreshTokenDao.CreateAsync(
            user.Id,
            tokenHash,
            DateTime.UtcNow.AddDays(7)
        );

        var userId = await this.refreshTokenDao.FindValidUserIdByTokenHashAsync(tokenHash);

        userId.Should().Be(user.Id);
    }

    [Test]
    public async Task FindValidUserIdByTokenHashAsync_ReturnsNull_WhenTokenIsExpired()
    {
        var user = await this.SeedConfirmedUserAsync();
        var tokenHash = HashToken("expired-token");

        await this.refreshTokenDao.CreateAsync(
            user.Id,
            tokenHash,
            DateTime.UtcNow.AddMinutes(-1)
        );

        var userId = await this.refreshTokenDao.FindValidUserIdByTokenHashAsync(tokenHash);

        userId.Should().BeNull();
    }

    [Test]
    public async Task RevokeByTokenHashAsync_IsIdempotent_WhenTokenAlreadyRevoked()
    {
        var user = await this.SeedConfirmedUserAsync();
        var tokenHash = HashToken("revoked-token");

        await this.refreshTokenDao.CreateAsync(
            user.Id,
            tokenHash,
            DateTime.UtcNow.AddDays(7)
        );

        await this.refreshTokenDao.RevokeByTokenHashAsync(tokenHash);
        await this.refreshTokenDao.RevokeByTokenHashAsync(tokenHash);

        var userId = await this.refreshTokenDao.FindValidUserIdByTokenHashAsync(tokenHash);

        userId.Should().BeNull();
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
