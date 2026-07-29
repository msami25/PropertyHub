using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyHub.Domain.Entities;
using PropertyHub.Infrastructure.Identity;

namespace PropertyHub.Infrastructure.Data.Configurations;

public sealed class SellerProfileConfiguration : IEntityTypeConfiguration<SellerProfile>
{
    public void Configure(EntityTypeBuilder<SellerProfile> builder)
    {
        builder.Property(profile => profile.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(profile => profile.PhoneNumber).HasMaxLength(20).IsRequired();
        builder.Property(profile => profile.CreatedAtUtc).HasPrecision(0);
        builder.Property(profile => profile.UpdatedAtUtc).HasPrecision(0);
        builder.HasIndex(profile => profile.UserId).IsUnique();
        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<SellerProfile>(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
