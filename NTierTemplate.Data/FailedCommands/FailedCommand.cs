using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NTierTemplate.Data.FailedCommands;

/// <summary>
/// Persistence model for a queue command that failed and may be retried.
/// </summary>
public class FailedCommand : IEntityTypeConfiguration<FailedCommand>
{
    public int Id { get; set; }

    public Guid MessageId { get; set; }

    public string CommandName { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public int AttemptCount { get; set; }

    public int MaxAttempts { get; set; }

    public string? LastError { get; set; }

    public FailedCommandStatus Status { get; set; }

    public DateTime FailedAtUtc { get; set; }

    public DateTime? NextRetryAtUtc { get; set; }

    /// <summary>
    /// Configure the FailedCommand table mapping.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<FailedCommand> builder)
    {
        builder.ToTable("FailedCommand", t => t.HasComment("Failed queue commands awaiting retry or manual review."));

        builder.HasKey(command => command.Id);
        builder.Property(command => command.Id).ValueGeneratedOnAdd();

        builder.Property(command => command.MessageId).IsRequired();
        builder.Property(command => command.CommandName).IsRequired().HasMaxLength(128);
        builder.Property(command => command.Payload).IsRequired();
        builder.Property(command => command.AttemptCount).IsRequired();
        builder.Property(command => command.MaxAttempts).IsRequired();
        builder.Property(command => command.LastError).HasMaxLength(2000);
        builder.Property(command => command.Status)
            .IsRequired()
            .HasMaxLength(32)
            .HasConversion<string>();
        builder.Property(command => command.FailedAtUtc).IsRequired();
        builder.Property(command => command.NextRetryAtUtc);

        builder.HasIndex(command => command.MessageId)
            .IsUnique()
            .HasDatabaseName("UX_FailedCommand_MessageId");

        builder.HasIndex(command => new { command.Status, command.NextRetryAtUtc })
            .HasDatabaseName("IX_FailedCommand_Status_NextRetryAtUtc");
    }
}
