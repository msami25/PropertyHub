using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyHub.Domain.Entities;

namespace PropertyHub.Infrastructure.Data.Configurations;

public sealed class PropertyImageConfiguration : IEntityTypeConfiguration<PropertyImage>
{
    public void Configure(EntityTypeBuilder<PropertyImage> builder)
    {
        builder.Property(image => image.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(image => image.StoredFileName).HasMaxLength(255).IsRequired();
        builder.Property(image => image.RelativePath).HasMaxLength(500).IsRequired();
        builder.Property(image => image.ContentType).HasMaxLength(50).IsRequired();
        builder.Property(image => image.Width).IsRequired();
        builder.Property(image => image.Height).IsRequired();
        builder.Property(image => image.UploadedAtUtc).HasPrecision(0);
        builder.HasIndex(image => image.StoredFileName).IsUnique();
        builder.HasIndex(image => image.RelativePath).IsUnique();
        builder.HasIndex(image => new { image.PropertyId, image.SortOrder }).IsUnique();
        builder.HasOne(image => image.Property)
            .WithMany(property => property.Images)
            .HasForeignKey(image => image.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
