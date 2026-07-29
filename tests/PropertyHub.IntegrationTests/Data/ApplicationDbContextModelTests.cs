using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PropertyHub.Domain.Entities;
using PropertyHub.IntegrationTests.Infrastructure;
using PropertyHub.Infrastructure.Data;

namespace PropertyHub.IntegrationTests.Data;

public sealed class ApplicationDbContextModelTests(PropertyHubWebApplicationFactory factory)
    : IClassFixture<PropertyHubWebApplicationFactory>
{
    [Fact]
    public void Model_ShouldEnforceCorePropertyRelationshipsAndIndexes()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var model = context.Model;

        var city = model.FindEntityType(typeof(City));
        var property = model.FindEntityType(typeof(Property));
        var image = model.FindEntityType(typeof(PropertyImage));

        city.Should().NotBeNull();
        property.Should().NotBeNull();
        image.Should().NotBeNull();

        city!.GetIndexes()
            .Single(index => index.Properties.Single().Name == nameof(City.NormalizedName))
            .IsUnique.Should().BeTrue();

        property!.GetForeignKeys()
            .Should().OnlyContain(foreignKey => foreignKey.DeleteBehavior == DeleteBehavior.Restrict);
        image!.GetForeignKeys()
            .Should().OnlyContain(foreignKey => foreignKey.DeleteBehavior == DeleteBehavior.Restrict);
    }
}
