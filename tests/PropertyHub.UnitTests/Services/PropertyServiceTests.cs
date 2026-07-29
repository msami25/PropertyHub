using FluentAssertions;
using Moq;
using PropertyHub.Application.Contracts.Properties;
using PropertyHub.Application.Interfaces.Repositories;
using PropertyHub.Application.Models.Auth;
using PropertyHub.Application.Models.Properties;
using PropertyHub.Application.Services;
using PropertyHub.Domain.Entities;
using PropertyHub.Domain.Enums;

namespace PropertyHub.UnitTests.Services;

public sealed class PropertyServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CityId = Guid.NewGuid();
    private readonly Mock<IPropertyRepository> _propertyRepository = new();
    private readonly Mock<IUserAccountRepository> _userRepository = new();

    [Fact]
    public async Task CreateAsync_ShouldRejectHouseWithoutPositiveRooms()
    {
        var service = CreateService();
        var request = ValidCreate() with { Bedrooms = null };

        var result = await service.CreateAsync(UserId, request, CancellationToken.None);

        result.Outcome.Should().Be(PropertyMutationOutcome.Invalid);
        result.Error.Should().Contain("positive bedroom");
        _propertyRepository.Verify(
            repository => repository.AddPropertyAsync(
                It.IsAny<Property>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldNormalizeConvertAreaAndStartPending()
    {
        var profile = new SellerProfile
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            DisplayName = "Owner",
            PhoneNumber = "03000000000"
        };
        Property? captured = null;
        _propertyRepository.Setup(repository => repository.ActiveCityExistsAsync(
                CityId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _propertyRepository.Setup(repository => repository.GetSellerProfileAsync(
                UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _propertyRepository.Setup(repository => repository.DuplicateExistsAsync(
                profile.Id,
                "FAMILY HOUSE",
                "DHA PHASE 6",
                PropertyPurpose.Sale,
                PropertyType.House,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _propertyRepository.Setup(repository => repository.AddPropertyAsync(
                It.IsAny<Property>(),
                It.IsAny<CancellationToken>()))
            .Callback<Property, CancellationToken>((property, _) =>
            {
                captured = property;
                property.City = new City { Id = CityId, Name = "Lahore" };
                property.SellerProfile = profile;
            })
            .Returns(Task.CompletedTask);
        _propertyRepository.Setup(repository => repository.GetOwnedAsync(
                It.IsAny<Guid>(),
                UserId,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => captured);
        var service = CreateService();

        var result = await service.CreateAsync(UserId, ValidCreate(), CancellationToken.None);

        result.Outcome.Should().Be(PropertyMutationOutcome.Success);
        captured!.NormalizedTitle.Should().Be("FAMILY HOUSE");
        captured.NormalizedAddress.Should().Be("DHA PHASE 6");
        captured.AreaSquareFeet.Should().Be(1361.25m);
        captured.ModerationStatus.Should().Be(ModerationStatus.Pending);
        captured.AvailabilityStatus.Should().Be(AvailabilityStatus.Available);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectDuplicateOwnerSignature()
    {
        var profile = new SellerProfile { Id = Guid.NewGuid(), UserId = UserId };
        _propertyRepository.Setup(repository => repository.ActiveCityExistsAsync(
                CityId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _propertyRepository.Setup(repository => repository.GetSellerProfileAsync(
                UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _propertyRepository.Setup(repository => repository.DuplicateExistsAsync(
                profile.Id,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<PropertyPurpose>(),
                It.IsAny<PropertyType>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = CreateService();

        var result = await service.CreateAsync(UserId, ValidCreate(), CancellationToken.None);

        result.Outcome.Should().Be(PropertyMutationOutcome.Duplicate);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnApprovedPropertyToPending()
    {
        var property = ExistingProperty();
        property.ModerationStatus = ModerationStatus.Approved;
        _propertyRepository.Setup(repository => repository.GetOwnedAsync(
                property.Id,
                UserId,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(property);
        _propertyRepository.Setup(repository => repository.ActiveCityExistsAsync(
                CityId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = CreateService();

        var result = await service.UpdateAsync(
            property.Id,
            UserId,
            ValidUpdate() with { Title = "Updated family house" },
            CancellationToken.None);

        result.Outcome.Should().Be(PropertyMutationOutcome.Success);
        property.ModerationStatus.Should().Be(ModerationStatus.Pending);
        property.Title.Should().Be("Updated family house");
    }

    [Fact]
    public async Task UpdateAvailabilityAsync_ShouldEnforcePurposeAndTerminalState()
    {
        var property = ExistingProperty();
        _propertyRepository.Setup(repository => repository.GetOwnedAsync(
                property.Id,
                UserId,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(property);
        var service = CreateService();

        var invalid = await service.UpdateAvailabilityAsync(
            property.Id,
            UserId,
            new UpdateAvailabilityRequest(AvailabilityStatus.Rented),
            CancellationToken.None);
        var valid = await service.UpdateAvailabilityAsync(
            property.Id,
            UserId,
            new UpdateAvailabilityRequest(AvailabilityStatus.Sold),
            CancellationToken.None);

        invalid.Outcome.Should().Be(PropertyMutationOutcome.InvalidTransition);
        valid.Outcome.Should().Be(PropertyMutationOutcome.Success);
        property.AvailabilityStatus.Should().Be(AvailabilityStatus.Sold);
    }

    [Fact]
    public async Task ModerateAsync_ShouldRequireReasonAndApprovePendingProperty()
    {
        var property = ExistingProperty();
        _propertyRepository.Setup(repository => repository.GetForModerationAsync(
                property.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(property);
        var service = CreateService();

        var invalid = await service.ModerateAsync(
            property.Id,
            Guid.NewGuid(),
            new ModeratePropertyRequest(ModerationStatus.Rejected, " "),
            CancellationToken.None);
        var approved = await service.ModerateAsync(
            property.Id,
            Guid.NewGuid(),
            new ModeratePropertyRequest(ModerationStatus.Approved, null),
            CancellationToken.None);

        invalid.Outcome.Should().Be(PropertyMutationOutcome.Invalid);
        approved.Outcome.Should().Be(PropertyMutationOutcome.Success);
        property.ModerationStatus.Should().Be(ModerationStatus.Approved);
    }

    private PropertyService CreateService()
    {
        _userRepository.Setup(repository => repository.GetByIdAsync(
                UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountSnapshot(
                UserId,
                "Owner",
                "owner@propertyhub.test",
                AccountStatus.Active,
                0,
                ["User"]));
        return new PropertyService(
            _propertyRepository.Object,
            _userRepository.Object,
            TimeProvider.System);
    }

    private static CreatePropertyRequest ValidCreate() =>
        new(
            " Family house ",
            "A complete family property description.",
            PropertyPurpose.Sale,
            PropertyType.House,
            CityId,
            " DHA Phase 6 ",
            25_000_000,
            5,
            AreaUnit.Marla,
            3,
            3,
            "03000000000");

    private static UpdatePropertyRequest ValidUpdate() =>
        new(
            "Family house",
            "A complete family property description.",
            PropertyPurpose.Sale,
            PropertyType.House,
            CityId,
            "DHA Phase 6",
            25_000_000,
            5,
            AreaUnit.Marla,
            3,
            3,
            "03000000000");

    private static Property ExistingProperty()
    {
        var profile = new SellerProfile
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            DisplayName = "Owner"
        };
        return new Property
        {
            Id = Guid.NewGuid(),
            SellerProfileId = profile.Id,
            SellerProfile = profile,
            CityId = CityId,
            City = new City { Id = CityId, Name = "Lahore" },
            Title = "Family house",
            NormalizedTitle = "FAMILY HOUSE",
            Description = "A complete family property description.",
            Purpose = PropertyPurpose.Sale,
            PropertyType = PropertyType.House,
            Address = "DHA Phase 6",
            NormalizedAddress = "DHA PHASE 6",
            Price = 25_000_000,
            Area = 5,
            AreaUnit = AreaUnit.Marla,
            AreaSquareFeet = 1361.25m,
            Bedrooms = 3,
            Bathrooms = 3,
            ContactNumber = "03000000000"
        };
    }
}
