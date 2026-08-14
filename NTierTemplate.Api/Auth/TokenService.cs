using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using NTierTemplate.Application.Auth;
using NTierTemplate.Application.Users;
using NTierTemplate.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace NTierTemplate.Api.Auth;

/// <summary>
/// Issues and validates JWT access tokens and refresh tokens.
/// </summary>
public class TokenService(
    IAuthApplicationService authApplicationService,
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
        // Compute access-token expiry and mint the JWT.
        var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(this.options.AccessTokenMinutes);
        var accessToken = this.CreateAccessToken(user, accessTokenExpiresAt);

        // Issue a new refresh token for the user.
        var refreshToken = await authApplicationService.IssueRefreshTokenAsync(
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
        // Redeem the refresh token and obtain the user id.
        var userId = await authApplicationService.RedeemRefreshTokenAsync(refreshToken, cancellationToken);

        if (userId == null)
        {
            return null;
        }

        // Load the user for token issuance.
        var user = await userApplicationService.GetByIdAsync(userId.Value, cancellationToken);

        if (user == null)
        {
            return null;
        }

        // Issue a fresh access and refresh token pair.
        return await this.CreateTokenPairAsync(user, cancellationToken);
    }

    /// <inheritdoc />
    public Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        return authApplicationService.RevokeRefreshTokenAsync(refreshToken, cancellationToken);
    }

    private string CreateAccessToken(User user, DateTime expiresAt)
    {
        // Build standard identity and JWT subject claims.
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
        };

        // Add one role claim per assigned role.
        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Sign the token with the configured key.
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
