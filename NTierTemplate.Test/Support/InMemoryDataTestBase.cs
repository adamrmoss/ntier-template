using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NTierTemplate.Data;
using NTierTemplate.Data.Users;

namespace NTierTemplate.Test.Support;

/// <summary>
/// Builds an isolated SQLite database with Identity for Data-layer tests.
/// </summary>
public abstract class InMemoryDataTestBase
{
    private ServiceProvider serviceProvider = null!;
    private SqliteConnection connection = null!;

    protected NTierTemplateDbContext DbContext { get; private set; } = null!;

    protected UserManager<ApplicationUser> UserManager { get; private set; } = null!;

    protected RoleManager<ApplicationRole> RoleManager { get; private set; } = null!;

    [SetUp]
    public void SetUpInMemoryData()
    {
        this.connection = new SqliteConnection("Data Source=:memory:");
        this.connection.Open();

        var services = new ServiceCollection();

        services.AddLogging();
        services.AddDataProtection();
        services.AddDbContext<NTierTemplateDbContext>(options =>
        {
            options.UseSqlite(this.connection);
        });
        services
            .AddIdentityCore<ApplicationUser>(identityOptions =>
            {
                identityOptions.User.RequireUniqueEmail = true;
                identityOptions.Password.RequiredLength = 8;
                identityOptions.Password.RequireDigit = true;
                identityOptions.Password.RequireLowercase = true;
                identityOptions.Password.RequireUppercase = true;
                identityOptions.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<NTierTemplateDbContext>()
            .AddDefaultTokenProviders();

        this.serviceProvider = services.BuildServiceProvider();
        this.DbContext = this.serviceProvider.GetRequiredService<NTierTemplateDbContext>();
        this.UserManager = this.serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        this.RoleManager = this.serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        this.DbContext.Database.EnsureCreated();
    }

    [TearDown]
    public async Task TearDownInMemoryData()
    {
        this.UserManager.Dispose();
        this.RoleManager.Dispose();
        await this.DbContext.DisposeAsync();
        await this.serviceProvider.DisposeAsync();
        await this.connection.DisposeAsync();
    }

    /// <summary>
    /// Create a confirmed standard user account in the test database.
    /// </summary>
    protected async Task<ApplicationUser> SeedConfirmedUserAsync(
        string email = "user@example.com",
        string password = "Password1"
    )
    {
        await this.EnsureRoleExistsAsync("User");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Test",
            LastName = "User",
        };

        var createResult = await this.UserManager.CreateAsync(user, password);

        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(error => error.Description)));
        }

        await this.UserManager.AddToRoleAsync(user, "User");

        return user;
    }

    /// <summary>
    /// Ensure a role exists in the test database.
    /// </summary>
    protected async Task EnsureRoleExistsAsync(string roleName)
    {
        if (!await this.RoleManager.RoleExistsAsync(roleName))
        {
            await this.RoleManager.CreateAsync(new ApplicationRole { Name = roleName });
        }
    }
}
