namespace PropertyHub.Application.Contracts.Admin;

public sealed record AdminDashboardResponse(
    DateTime AsOfUtc,
    AdminUserMetricsResponse Users,
    AdminPropertyMetricsResponse Properties,
    int TotalCities);

public sealed record AdminUserMetricsResponse(
    int Total,
    int Registered,
    int Active,
    int Disabled);

public sealed record AdminPropertyMetricsResponse(
    int Total,
    int Pending,
    int Approved,
    int Rejected);
