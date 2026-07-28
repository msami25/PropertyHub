namespace PropertyHub.Application.Contracts.Cities;

public sealed record CityResponse(
    Guid Id,
    string Name,
    bool IsActive,
    decimal Latitude,
    decimal Longitude);
