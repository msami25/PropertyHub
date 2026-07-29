namespace PropertyHub.Application.Models.Weather;

public sealed record CurrentWeather(
    decimal TemperatureCelsius,
    int RelativeHumidityPercent,
    decimal WindSpeedKilometresPerHour,
    string Condition,
    DateTime ObservedAtUtc);
