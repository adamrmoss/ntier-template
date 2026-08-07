using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using NTierTemplate.Application.Users;
using NTierTemplate.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace NTierTemplate.Api.Auth;

/// <summary>
/// Issues and validates JWT access tokens and refresh tokens.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Create access and refresh tokens for a signed-in user.
    /// </summary>
    /// <param name="user">The authenticated user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Token response.</returns>
    Task<TokenResponse> CreateTokenPairAsync(
        User user,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Rotate a refresh token and issue a new access token.
    /// </summary>
    /// <param name="refreshToken">The raw refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Token response when valid; otherwise null.</returns>
    Task<TokenResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke a refresh token if it exists.
    /// </summary>
    /// <param name="refreshToken">The raw refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}

/// <summary>
/// Issues and validates JWT access tokens and refresh tokens.
/// </summary>
public class TokenService(
    IUserApplicationService userApplicationService,
    IOptions<JwtOptions> jwtOptions
)
    : ITokenService
{
    private readonly JwtOptions options = jwtOptions.Value;

    /// <inheritdoc />
    public async Task<TokenResponse> CreateTokenPairAsync(
        User user,
        CancellationToken cancellationToken = default
    )
    {
        var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(this.options.AccessTokenMinutes);
        var accessToken = this.CreateAccessToken(user, accessTokenExpiresAt);
        var refreshToken = await userApplicationService.IssueRefreshTokenAsync(
            user.Id,
            this.options.RefreshTokenDays,
            cancellationToken
        );

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = accessTokenExpiresAt,
        };
    }

    /// <inheritdoc />
    public async Task<TokenResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var userId = await userApplicationService.RedeemRefreshTokenAsync(refreshToken, cancellationToken);

        if (userId == null)
        {
            return null;
        }

        var user = await userApplicationService.GetByIdAsync(userId.Value, cancellationToken);

        if (user == null)
        {
            return null;
        }

        return await this.CreateTokenPairAsync(user, cancellationToken);
    }

    /// <inheritdoc />
    public Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        return userApplicationService.RevokeRefreshTokenAsync(refreshToken, cancellationToken);
    }

    private string CreateAccessToken(User user, DateTime expiresAt)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
        };

        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this.options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: this.options.Issuer,
            audience: this.options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
