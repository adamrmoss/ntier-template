using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NTierTemplate.Data.Users;

namespace NTierTemplate.Data;

/// <summary>
/// Entity Framework database context for NTierTemplate.
/// </summary>
public class NTierTemplateDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
{
    /// <summary>
    /// Create a database context with the given options.
    /// </summary>
    /// <param name="options">The configured DbContext options.</param>
    public NTierTemplateDbContext(DbContextOptions<NTierTemplateDbContext> options)
        : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshToken { get; set; }

    /// <summary>
    /// Apply entity configurations defined on persistence types in this assembly.
    /// </summary>
    /// <param name="modelBuilder">The model builder for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<IdentityUserRole<int>>().ToTable("UserRole");
        modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("UserClaim");
        modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("UserLogin");
        modelBuilder.Entity<IdentityUserToken<int>>().ToTable("UserToken");
        modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaim");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NTierTemplateDbContext).Assembly);
    }
}
