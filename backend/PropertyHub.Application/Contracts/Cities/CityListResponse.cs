namespace PropertyHub.Application.Contracts.Cities;

public sealed record CityListResponse(IReadOnlyList<CityResponse> Items);
