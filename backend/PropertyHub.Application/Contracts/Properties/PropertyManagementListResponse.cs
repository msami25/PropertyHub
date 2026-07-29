namespace PropertyHub.Application.Contracts.Properties;

public sealed record PropertyManagementListResponse(IReadOnlyList<PropertyManagementResponse> Items);
