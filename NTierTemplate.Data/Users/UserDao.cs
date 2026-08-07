using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NTierTemplate.Users;

namespace NTierTemplate.Data.Users;

/// <summary>
/// Data access for application account users.
/// </summary>
public class UserDao(
    NTierTemplateDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager
)
    : DaoBase, IUserDao
{
    /// <inheritdoc />
    public async Task<User[]> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await dbContext.Users
            .OrderBy(user => user.DisplayName ?? user.Email)
            .ToArrayAsync(cancellationToken);

        return await this.MapUsersAsync(users, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);

        if (user == null)
        {
            return null;
        }

        return await this.MapUserAsync(user, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user == null)
        {
            return null;
        }

        return await this.MapUserAsync(user, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetByPrincipalAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default
    )
    {
        var user = await userManager.GetUserAsync(principal);

        if (user == null)
        {
            return null;
        }

        return await this.MapUserAsync(user, cancellationToken);
    }

    /// <inheritdoc />
    public async Task EnsureDefaultRolesExistAsync(CancellationToken cancellationToken = default)
    {
        await this.EnsureRoleExistsAsync("Admin");
        await this.EnsureRoleExistsAsync("User");
    }

    /// <inheritdoc />
    public async Task<UserCreateResult> CreateAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var user = new ApplicationUser
        {
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim(),
            FirstName = request.FirstName ?? string.Empty,
            LastName = request.LastName ?? string.Empty,
            EmailConfirmed = false,
        };

        var createResult = await userManager.CreateAsync(user, request.Password);

        if (!createResult.Succeeded)
        {
            return new UserCreateResult
            {
                Succeeded = false,
                Errors = createResult.Errors.Select(error => error.Description).ToArray(),
            };
        }

        await this.EnsureRoleExistsAsync("User");
        await userManager.AddToRoleAsync(user, "User");

        var domainUser = await this.MapUserAsync(user, cancellationToken);

        return new UserCreateResult
        {
            Succeeded = true,
            User = domainUser,
        };
    }

    /// <inheritdoc />
    public async Task<UserCreateResult> CreateAdminAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default
    )
    {
        await this.EnsureDefaultRolesExistAsync(cancellationToken);

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
        };

        var createResult = await userManager.CreateAsync(user, password);

        if (!createResult.Succeeded)
        {
            return new UserCreateResult
            {
                Succeeded = false,
                Errors = createResult.Errors.Select(error => error.Description).ToArray(),
            };
        }

        await userManager.AddToRoleAsync(user, "Admin");
        await userManager.AddToRoleAsync(user, "User");

        var domainUser = await this.MapUserAsync(user, cancellationToken);

        return new UserCreateResult
        {
            Succeeded = true,
            User = domainUser,
        };
    }

    /// <inheritdoc />
    public async Task<User?> ValidatePasswordAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default
    )
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user == null)
        {
            return null;
        }

        var passwordValid = await userManager.CheckPasswordAsync(user, password);

        if (!passwordValid)
        {
            return null;
        }

        if (!user.EmailConfirmed)
        {
            return null;
        }

        return await this.MapUserAsync(user, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> CheckPasswordAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default
    )
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user == null)
        {
            return false;
        }

        return await userManager.CheckPasswordAsync(user, password);
    }

    /// <inheritdoc />
    public async Task<bool> IsEmailConfirmedAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);

        return user?.EmailConfirmed == true;
    }

    /// <inheritdoc />
    public async Task<string?> GenerateEmailConfirmationTokenAsync(
        int userId,
        CancellationToken cancellationToken = default
    )
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user == null)
        {
            return null;
        }

        return await userManager.GenerateEmailConfirmationTokenAsync(user);
    }

    /// <inheritdoc />
    public async Task<AuthOperationResult> ConfirmEmailAsync(
        int userId,
        string token,
        CancellationToken cancellationToken = default
    )
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user == null)
        {
            return new AuthOperationResult
            {
                Succeeded = false,
                Errors = ["Invalid confirmation link."],
            };
        }

        var result = await userManager.ConfirmEmailAsync(user, token);

        if (!result.Succeeded)
        {
            return new AuthOperationResult
            {
                Succeeded = false,
                Errors = result.Errors.Select(error => error.Description).ToArray(),
            };
        }

        return new AuthOperationResult
        {
            Succeeded = true,
            User = await this.MapUserAsync(user, cancellationToken),
        };
    }

    /// <inheritdoc />
    public async Task<string?> GeneratePasswordResetTokenAsync(
        string email,
        CancellationToken cancellationToken = default
    )
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user == null)
        {
            return null;
        }

        return await userManager.GeneratePasswordResetTokenAsync(user);
    }

    /// <inheritdoc />
    public async Task<AuthOperationResult> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default
    )
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user == null)
        {
            return new AuthOperationResult
            {
                Succeeded = false,
                Errors = ["Invalid or expired reset link."],
            };
        }

        var result = await userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
        {
            return new AuthOperationResult
            {
                Succeeded = false,
                Errors = result.Errors.Select(error => error.Description).ToArray(),
            };
        }

        return new AuthOperationResult
        {
            Succeeded = true,
            User = await this.MapUserAsync(user, cancellationToken),
        };
    }

    private async Task<User[]> MapUsersAsync(
        ApplicationUser[] entities,
        CancellationToken cancellationToken
    )
    {
        var results = new List<User>(entities.Length);

        foreach (var entity in entities)
        {
            results.Add(await this.MapUserAsync(entity, cancellationToken));
        }

        return results.ToArray();
    }

    private async Task<User> MapUserAsync(
        ApplicationUser entity,
        CancellationToken cancellationToken
    )
    {
        var roles = await this.GetRoleNamesAsync(entity.Id, cancellationToken);

        return new User
        {
            Id = entity.Id,
            Email = entity.Email ?? string.Empty,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            DisplayName = this.ResolveDisplayName(entity),
            Roles = roles,
        };
    }

    private string ResolveDisplayName(ApplicationUser entity)
    {
        if (!string.IsNullOrWhiteSpace(entity.DisplayName))
        {
            return entity.DisplayName;
        }

        return entity.Email ?? string.Empty;
    }

    private async Task<string[]> GetRoleNamesAsync(int userId, CancellationToken cancellationToken)
    {
        return await dbContext.UserRoles
            .Where(userRole => userRole.UserId == userId)
            .Join(
                dbContext.Roles,
                userRole => userRole.RoleId,
                role => role.Id,
                (userRole, role) => role.Name!
            )
            .OrderBy(roleName => roleName)
            .ToArrayAsync(cancellationToken);
    }

    private async Task EnsureRoleExistsAsync(string roleName)
    {
        if (await roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        await roleManager.CreateAsync(new ApplicationRole { Name = roleName });
    }
}
