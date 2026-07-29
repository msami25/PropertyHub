using FluentAssertions;
using Moq;
using PropertyHub.Application.Interfaces.Repositories;
using PropertyHub.Application.Interfaces.Services;
using PropertyHub.Application.Models.Weather;
using PropertyHub.Application.Services;
using PropertyHub.Domain.Entities;

namespace PropertyHub.UnitTests.Services;

public sealed class PropertyWeatherServiceTests
{
    private readonly Mock<IPropertyRepository> _propertyRepository = new();
    private readonly Mock<IOpenMeteoWeatherClient> _weatherClient = new();

    [Fact]
    public async Task GetAsync_ShouldUsePublicPropertyCityCoordinates()
    {
        var property = PublicProperty();
        var current = new CurrentWeather(
            31.5m,
            62,
            8.4m,
            "Partly cloudy",
            new DateTime(2026, 7, 29, 6, 0, 0, DateTimeKind.Utc));
        _propertyRepository.Setup(repository => repository.GetPublicByIdAsync(
                property.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(property);
        _weatherClient.Setup(client => client.GetCurrentAsync(
                property.CityId,
                property.City.Latitude,
                property.City.Longitude,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(current);
        var service = new PropertyWeatherService(
            _propertyRepository.Object,
            _weatherClient.Object);

        var result = await service.GetAsync(property.Id, CancellationToken.None);

        result.Outcome.Should().Be(PropertyWeatherOutcome.Success);
        result.Weather!.IsAvailable.Should().BeTrue();
        result.Weather.Condition.Should().Be("Partly cloudy");
        result.Weather.TemperatureCelsius.Should().Be(31.5m);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnFriendlyFallbackWhenWeatherIsUnavailable()
    {
        var property = PublicProperty();
        _propertyRepository.Setup(repository => repository.GetPublicByIdAsync(
                property.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(property);
        _weatherClient.Setup(client => client.GetCurrentAsync(
                It.IsAny<Guid>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CurrentWeather?)null);
        var service = new PropertyWeatherService(
            _propertyRepository.Object,
            _weatherClient.Object);

        var result = await service.GetAsync(property.Id, CancellationToken.None);

        result.Outcome.Should().Be(PropertyWeatherOutcome.Success);
        result.Weather.Should().BeEquivalentTo(
            PropertyHub.Application.Contracts.Weather.PropertyWeatherResponse.Unavailable);
    }

    [Fact]
    public async Task GetAsync_ShouldNotCallProviderForHiddenProperty()
    {
        _propertyRepository.Setup(repository => repository.GetPublicByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Property?)null);
        var service = new PropertyWeatherService(
            _propertyRepository.Object,
            _weatherClient.Object);

        var result = await service.GetAsync(Guid.NewGuid(), CancellationToken.None);

        result.Outcome.Should().Be(PropertyWeatherOutcome.NotFound);
        _weatherClient.Verify(
            client => client.GetCurrentAsync(
                It.IsAny<Guid>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Property PublicProperty()
    {
        var city = new City
        {
            Id = Guid.NewGuid(),
            Name = "Lahore",
            Latitude = 31.520400m,
            Longitude = 74.358700m
        };
        return new Property
        {
            Id = Guid.NewGuid(),
            CityId = city.Id,
            City = city
        };
    }
}
