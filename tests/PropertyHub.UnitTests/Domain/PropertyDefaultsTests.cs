using FluentAssertions;
using PropertyHub.Domain.Entities;
using PropertyHub.Domain.Enums;

namespace PropertyHub.UnitTests.Domain;

public sealed class PropertyDefaultsTests
{
    [Fact]
    public void NewProperty_ShouldBePendingAndAvailable()
    {
        var property = new Property();

        property.ModerationStatus.Should().Be(ModerationStatus.Pending);
        property.AvailabilityStatus.Should().Be(AvailabilityStatus.Available);
        property.IsDeleted.Should().BeFalse();
    }
}
