using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NTierTemplate.Data.Users;

/// <summary>
/// Persistence model for an application account user (ASP.NET Core Identity).
/// </summary>
public class ApplicationUser : IdentityUser<int>, IEntityTypeConfiguration<ApplicationUser>
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    /// <summary>
    /// Configure the ApplicationUser table mapping.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("User", t => t.HasComment("Application account users."));
        builder.Property(user => user.DisplayName).HasMaxLength(128);
        builder.Property(user => user.FirstName).IsRequired().HasMaxLength(64);
        builder.Property(user => user.LastName).IsRequired().HasMaxLength(64);
        builder.HasIndex(user => user.NormalizedEmail).HasDatabaseName("IX_User_NormalizedEmail");
    }
}
