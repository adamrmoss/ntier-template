using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NTierTemplate.Data.Users;

/// <summary>
/// Persistence model for an application role.
/// </summary>
public class ApplicationRole : IdentityRole<int>, IEntityTypeConfiguration<ApplicationRole>
{
    /// <summary>
    /// Configure the ApplicationRole table mapping.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.ToTable("Role", t => t.HasComment("Application account roles."));
    }
}
