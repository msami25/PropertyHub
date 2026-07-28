using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyHub.Domain.Entities;

namespace PropertyHub.Infrastructure.Data.Configurations;

public sealed class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.Property(city => city.Name).HasMaxLength(100).IsRequired();
        builder.Property(city => city.NormalizedName).HasMaxLength(100).IsRequired();
        builder.HasIndex(city => city.NormalizedName).IsUnique();
        builder.Property(city => city.Latitude).HasPrecision(9, 6);
        builder.Property(city => city.Longitude).HasPrecision(9, 6);
    }
}
