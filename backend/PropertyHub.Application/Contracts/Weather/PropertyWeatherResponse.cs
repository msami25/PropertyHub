namespace PropertyHub.Application.Contracts.Weather;

public sealed record PropertyWeatherResponse(
    bool IsAvailable,
    decimal? TemperatureCelsius,
    int? RelativeHumidityPercent,
    decimal? WindSpeedKilometresPerHour,
    string? Condition,
    DateTime? ObservedAtUtc)
{
    public static PropertyWeatherResponse Unavailable { get; } =
        new(false, null, null, null, null, null);
}
