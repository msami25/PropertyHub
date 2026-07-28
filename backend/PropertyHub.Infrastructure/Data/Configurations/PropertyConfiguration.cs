using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyHub.Domain.Entities;

namespace PropertyHub.Infrastructure.Data.Configurations;

public sealed class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.Property(property => property.Title).HasMaxLength(100).IsRequired();
        builder.Property(property => property.Description).HasMaxLength(2000).IsRequired();
        builder.Property(property => property.Purpose).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(property => property.PropertyType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(property => property.Address).HasMaxLength(250).IsRequired();
        builder.Property(property => property.Price).HasPrecision(18, 2);
        builder.Property(property => property.Area).HasPrecision(12, 2);
        builder.Property(property => property.AreaUnit).HasMaxLength(20).IsRequired();
        builder.Property(property => property.ContactNumber).HasMaxLength(20).IsRequired();
        builder.Property(property => property.ModerationStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(property => property.AvailabilityStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(property => property.CreatedAtUtc).HasPrecision(0);
        builder.Property(property => property.UpdatedAtUtc).HasPrecision(0);
        builder.Property(property => property.DeletedAtUtc).HasPrecision(0);
        builder.HasIndex(property => property.SellerProfileId);
        builder.HasIndex(property => property.CityId);
        builder.HasOne(property => property.SellerProfile)
            .WithMany(profile => profile.Properties)
            .HasForeignKey(property => property.SellerProfileId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(property => property.City)
            .WithMany(city => city.Properties)
            .HasForeignKey(property => property.CityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
