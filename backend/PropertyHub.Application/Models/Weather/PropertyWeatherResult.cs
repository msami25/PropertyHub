using PropertyHub.Application.Contracts.Weather;

namespace PropertyHub.Application.Models.Weather;

public sealed record PropertyWeatherResult(
    PropertyWeatherOutcome Outcome,
    PropertyWeatherResponse? Weather = null);

public enum PropertyWeatherOutcome
{
    Success,
    NotFound
}
