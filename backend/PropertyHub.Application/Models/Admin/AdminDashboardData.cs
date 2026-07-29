namespace PropertyHub.Application.Models.Admin;

public sealed record AdminDashboardData(
    int TotalUsers,
    int RegisteredUsers,
    int ActiveUsers,
    int DisabledUsers,
    int TotalProperties,
    int PendingProperties,
    int ApprovedProperties,
    int RejectedProperties,
    int TotalCities);
