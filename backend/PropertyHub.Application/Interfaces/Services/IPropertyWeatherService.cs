using PropertyHub.Application.Models.Weather;

namespace PropertyHub.Application.Interfaces.Services;

public interface IPropertyWeatherService
{
    Task<PropertyWeatherResult> GetAsync(
        Guid propertyId,
        CancellationToken cancellationToken);
}
