using PropertyHub.Application.Models.Weather;

namespace PropertyHub.Application.Interfaces.Services;

public interface IOpenMeteoWeatherClient
{
    Task<CurrentWeather?> GetCurrentAsync(
        Guid cityId,
        decimal latitude,
        decimal longitude,
        CancellationToken cancellationToken);
}
