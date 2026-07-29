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
        builder.HasData(
            new
            {
                Id = Guid.Parse("10000000-0000-4000-8000-000000000001"),
                Name = "Lahore",
                NormalizedName = "LAHORE",
                IsActive = true,
                Latitude = 31.520400m,
                Longitude = 74.358700m
            },
            new
            {
                Id = Guid.Parse("10000000-0000-4000-8000-000000000002"),
                Name = "Karachi",
                NormalizedName = "KARACHI",
                IsActive = true,
                Latitude = 24.860700m,
                Longitude = 67.001100m
            },
            new
            {
                Id = Guid.Parse("10000000-0000-4000-8000-000000000003"),
                Name = "Islamabad",
                NormalizedName = "ISLAMABAD",
                IsActive = true,
                Latitude = 33.684400m,
                Longitude = 73.047900m
            },
            new
            {
                Id = Guid.Parse("10000000-0000-4000-8000-000000000004"),
                Name = "Rawalpindi",
                NormalizedName = "RAWALPINDI",
                IsActive = true,
                Latitude = 33.565100m,
                Longitude = 73.016900m
            },
            new
            {
                Id = Guid.Parse("10000000-0000-4000-8000-000000000005"),
                Name = "Faisalabad",
                NormalizedName = "FAISALABAD",
                IsActive = true,
                Latitude = 31.450400m,
                Longitude = 73.135000m
            },
            new
            {
                Id = Guid.Parse("10000000-0000-4000-8000-000000000006"),
                Name = "Multan",
                NormalizedName = "MULTAN",
                IsActive = true,
                Latitude = 30.157500m,
                Longitude = 71.524900m
            },
            new
            {
                Id = Guid.Parse("10000000-0000-4000-8000-000000000007"),
                Name = "Peshawar",
                NormalizedName = "PESHAWAR",
                IsActive = true,
                Latitude = 34.015100m,
                Longitude = 71.524900m
            },
            new
            {
                Id = Guid.Parse("10000000-0000-4000-8000-000000000008"),
                Name = "Quetta",
                NormalizedName = "QUETTA",
                IsActive = true,
                Latitude = 30.179800m,
                Longitude = 66.975000m
            });
    }
}
