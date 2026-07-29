namespace PropertyHub.Application.Contracts.Properties;

public sealed record PropertyListResponse(IReadOnlyList<PropertySummaryResponse> Items);
