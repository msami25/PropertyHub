using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyHub.Domain.Entities;
using PropertyHub.Infrastructure.Identity;

namespace PropertyHub.Infrastructure.Data.Configurations;

public sealed class UserStatusChangeConfiguration : IEntityTypeConfiguration<UserStatusChange>
{
    public void Configure(EntityTypeBuilder<UserStatusChange> builder)
    {
        builder.HasKey(change => change.Id);
        builder.Property(change => change.PreviousStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(change => change.NewStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(change => change.Reason).HasMaxLength(500).IsRequired();
        builder.Property(change => change.CreatedAtUtc).HasPrecision(0);
        builder.Property(change => change.CorrelationId).HasMaxLength(100).IsRequired();
        builder.HasIndex(change => change.TargetUserId);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(change => change.TargetUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(change => change.AdminUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
