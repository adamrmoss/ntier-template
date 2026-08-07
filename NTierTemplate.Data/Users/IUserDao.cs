using System.Security.Claims;
using NTierTemplate.Users;

namespace NTierTemplate.Data.Users;

/// <summary>
/// Data access for application account users.
/// </summary>
public interface IUserDao
{
    Task<User[]> GetAllAsync(CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByPrincipalAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);

    Task EnsureDefaultRolesExistAsync(CancellationToken cancellationToken = default);

    Task<UserCreateResult> CreateAsync(RegisterUserRequest request, CancellationToken cancellationToken = default);

    Task<UserCreateResult> CreateAdminAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<User?> ValidatePasswordAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<bool> CheckPasswordAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<bool> IsEmailConfirmedAsync(string email, CancellationToken cancellationToken = default);

    Task<string?> GenerateEmailConfirmationTokenAsync(int userId, CancellationToken cancellationToken = default);

    Task<AuthOperationResult> ConfirmEmailAsync(int userId, string token, CancellationToken cancellationToken = default);

    Task<string?> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken = default);

    Task<AuthOperationResult> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default
    );
}
