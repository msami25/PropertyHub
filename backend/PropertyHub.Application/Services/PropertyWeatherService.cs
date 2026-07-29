using PropertyHub.Application.Contracts.Weather;
using PropertyHub.Application.Interfaces.Repositories;
using PropertyHub.Application.Interfaces.Services;
using PropertyHub.Application.Models.Weather;

namespace PropertyHub.Application.Services;

public sealed class PropertyWeatherService(
    IPropertyRepository propertyRepository,
    IOpenMeteoWeatherClient weatherClient) : IPropertyWeatherService
{
    public async Task<PropertyWeatherResult> GetAsync(
        Guid propertyId,
        CancellationToken cancellationToken)
    {
        var property = await propertyRepository.GetPublicByIdAsync(propertyId, cancellationToken);
        if (property is null)
        {
            return new PropertyWeatherResult(PropertyWeatherOutcome.NotFound);
        }

        var current = await weatherClient.GetCurrentAsync(
            property.CityId,
            property.City.Latitude,
            property.City.Longitude,
            cancellationToken);
        var response = current is null
            ? PropertyWeatherResponse.Unavailable
            : new PropertyWeatherResponse(
                true,
                current.TemperatureCelsius,
                current.RelativeHumidityPercent,
                current.WindSpeedKilometresPerHour,
                current.Condition,
                current.ObservedAtUtc);
        return new PropertyWeatherResult(PropertyWeatherOutcome.Success, response);
    }
}
