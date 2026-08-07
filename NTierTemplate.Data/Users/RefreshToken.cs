using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NTierTemplate.Data.Users;

/// <summary>
/// Persistence model for a refresh token issued during login.
/// </summary>
public class RefreshToken : IEntityTypeConfiguration<RefreshToken>
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Configure the RefreshToken table mapping.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshToken", t => t.HasComment("Rotating refresh tokens for bearer authentication."));

        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).ValueGeneratedOnAdd();

        builder.Property(token => token.TokenHash).IsRequired().HasMaxLength(128);
        builder.Property(token => token.CreatedAt).IsRequired();
        builder.Property(token => token.ExpiresAt).IsRequired();
        builder.Property(token => token.RevokedAt);

        builder.HasIndex(token => token.TokenHash).HasDatabaseName("IX_RefreshToken_TokenHash");
        builder.HasIndex(token => token.UserId).HasDatabaseName("IX_RefreshToken_UserId");

        builder.HasOne(token => token.User)
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
