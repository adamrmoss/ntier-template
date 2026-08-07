using System.Security.Cryptography;
using System.Text;
using NTierTemplate.Data.Users;
using NTierTemplate.Users;

namespace NTierTemplate.Application.Users;

/// <summary>
/// Application use cases for account users.
/// </summary>
public class UserApplicationService(
    IUserDao userDao,
    IRefreshTokenDao refreshTokenDao,
    IPrincipalContainer principalContainer
)
    : IUserApplicationService
{
    /// <inheritdoc />
    public Task EnsureDefaultRolesExistAsync(CancellationToken cancellationToken = default)
    {
        return userDao.EnsureDefaultRolesExistAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserCreateResult> RegisterAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var existingUser = await userDao.GetByEmailAsync(request.Email, cancellationToken);

        if (existingUser != null)
        {
            return new UserCreateResult
            {
                Succeeded = false,
                Errors = ["An account with that email already exists."],
            };
        }

        var normalizedRequest = new RegisterUserRequest
        {
            Email = request.Email.Trim(),
            Password = request.Password,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim(),
            FirstName = string.IsNullOrWhiteSpace(request.FirstName) ? string.Empty : request.FirstName.Trim(),
            LastName = string.IsNullOrWhiteSpace(request.LastName) ? string.Empty : request.LastName.Trim(),
        };

        return await userDao.CreateAsync(normalizedRequest, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserCreateResult> CreateAdminAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default
    )
    {
        var existingUser = await userDao.GetByEmailAsync(email, cancellationToken);

        if (existingUser != null)
        {
            return new UserCreateResult
            {
                Succeeded = false,
                Errors = ["A user with that email already exists."],
            };
        }

        return await userDao.CreateAdminAsync(email, password, cancellationToken);
    }

    /// <inheritdoc />
    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return userDao.GetByIdAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return userDao.GetByEmailAsync(email, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var principal = principalContainer.Principal;

        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return await userDao.GetByPrincipalAsync(principal, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> ValidatePasswordAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default
    )
    {
        var user = await userDao.ValidatePasswordAsync(email, password, cancellationToken);

        if (user is not null)
        {
            principalContainer.SignIn(user);
        }

        return user;
    }

    /// <inheritdoc />
    public Task<bool> CheckPasswordAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default
    )
    {
        return userDao.CheckPasswordAsync(email, password, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> IsEmailConfirmedAsync(string email, CancellationToken cancellationToken = default)
    {
        return userDao.IsEmailConfirmedAsync(email, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string?> GenerateEmailConfirmationTokenAsync(
        int userId,
        CancellationToken cancellationToken = default
    )
    {
        return userDao.GenerateEmailConfirmationTokenAsync(userId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AuthOperationResult> ConfirmEmailAsync(
        int userId,
        string token,
        CancellationToken cancellationToken = default
    )
    {
        return userDao.ConfirmEmailAsync(userId, token, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string?> GeneratePasswordResetTokenAsync(
        string email,
        CancellationToken cancellationToken = default
    )
    {
        return userDao.GeneratePasswordResetTokenAsync(email, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AuthOperationResult> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default
    )
    {
        return userDao.ResetPasswordAsync(email, token, newPassword, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> IssueRefreshTokenAsync(
        int userId,
        int refreshTokenDays,
        CancellationToken cancellationToken = default
    )
    {
        var rawToken = GenerateSecureToken();
        var tokenHash = HashToken(rawToken);
        var expiresAt = DateTime.UtcNow.AddDays(refreshTokenDays);

        await refreshTokenDao.CreateAsync(userId, tokenHash, expiresAt, cancellationToken);

        return rawToken;
    }

    /// <inheritdoc />
    public async Task<int?> RedeemRefreshTokenAsync(
        string rawToken,
        CancellationToken cancellationToken = default
    )
    {
        var tokenHash = HashToken(rawToken);
        var userId = await refreshTokenDao.FindValidUserIdByTokenHashAsync(tokenHash, cancellationToken);

        if (userId == null)
        {
            return null;
        }

        await refreshTokenDao.RevokeByTokenHashAsync(tokenHash, cancellationToken);

        return userId;
    }

    /// <inheritdoc />
    public Task RevokeRefreshTokenAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        return refreshTokenDao.RevokeByTokenHashAsync(HashToken(rawToken), cancellationToken);
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
