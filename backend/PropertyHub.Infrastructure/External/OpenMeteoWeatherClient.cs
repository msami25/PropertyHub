using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PropertyHub.Application.Interfaces.Services;
using PropertyHub.Application.Models.Weather;

namespace PropertyHub.Infrastructure.External;

public sealed class OpenMeteoWeatherClient(
    HttpClient httpClient,
    IMemoryCache cache,
    OpenMeteoOptions options,
    ILogger<OpenMeteoWeatherClient> logger) : IOpenMeteoWeatherClient
{
    public async Task<CurrentWeather?> GetCurrentAsync(
        Guid cityId,
        decimal latitude,
        decimal longitude,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"weather:city:{cityId:N}";
        if (cache.TryGetValue(cacheKey, out CurrentWeather? cached) && cached is not null)
        {
            return cached;
        }

        try
        {
            var latitudeValue = latitude.ToString("0.######", CultureInfo.InvariantCulture);
            var longitudeValue = longitude.ToString("0.######", CultureInfo.InvariantCulture);
            var path = string.Concat(
                "v1/forecast?latitude=", latitudeValue,
                "&longitude=", longitudeValue,
                "&current=temperature_2m,relative_humidity_2m,weather_code,wind_speed_10m",
                "&timezone=UTC&forecast_days=1");
            var response = await httpClient.GetAsync(path, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Current weather returned status code {StatusCode} for city {CityId}",
                    (int)response.StatusCode,
                    cityId);
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<OpenMeteoResponse>(
                cancellationToken: cancellationToken);
            var weather = Map(payload);
            if (weather is null)
            {
                logger.LogWarning("Current weather returned an invalid payload for city {CityId}", cityId);
                return null;
            }

            cache.Set(cacheKey, weather, options.CacheDuration);
            return weather;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Current weather timed out for city {CityId}", cityId);
            return null;
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "Current weather request failed for city {CityId}",
                cityId);
            return null;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(
                exception,
                "Current weather response could not be read for city {CityId}",
                cityId);
            return null;
        }
        catch (NotSupportedException exception)
        {
            logger.LogWarning(
                exception,
                "Current weather response used an unsupported format for city {CityId}",
                cityId);
            return null;
        }
    }

    private static CurrentWeather? Map(OpenMeteoResponse? payload)
    {
        var current = payload?.Current;
        if (current is null
            || current.TemperatureCelsius is < -100 or > 100
            || current.RelativeHumidityPercent is < 0 or > 100
            || current.WindSpeedKilometresPerHour is < 0 or > 500
            || !DateTime.TryParse(
                current.Time,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var observedAtUtc))
        {
            return null;
        }

        return new CurrentWeather(
            current.TemperatureCelsius,
            current.RelativeHumidityPercent,
            current.WindSpeedKilometresPerHour,
            Describe(current.WeatherCode),
            DateTime.SpecifyKind(observedAtUtc, DateTimeKind.Utc));
    }

    private static string Describe(int code) =>
        code switch
        {
            0 => "Clear",
            1 => "Mainly clear",
            2 => "Partly cloudy",
            3 => "Overcast",
            45 or 48 => "Fog",
            51 or 53 or 55 => "Drizzle",
            56 or 57 => "Freezing drizzle",
            61 or 63 or 65 => "Rain",
            66 or 67 => "Freezing rain",
            71 or 73 or 75 or 77 => "Snow",
            80 or 81 or 82 => "Rain showers",
            85 or 86 => "Snow showers",
            95 => "Thunderstorm",
            96 or 99 => "Thunderstorm with hail",
            _ => "Current conditions"
        };

    private sealed record OpenMeteoResponse(
        [property: JsonPropertyName("current")] OpenMeteoCurrent? Current);

    private sealed record OpenMeteoCurrent(
        [property: JsonPropertyName("time")] string? Time,
        [property: JsonPropertyName("temperature_2m")] decimal TemperatureCelsius,
        [property: JsonPropertyName("relative_humidity_2m")] int RelativeHumidityPercent,
        [property: JsonPropertyName("weather_code")] int WeatherCode,
        [property: JsonPropertyName("wind_speed_10m")] decimal WindSpeedKilometresPerHour);
}
