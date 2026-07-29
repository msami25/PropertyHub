namespace PropertyHub.Application.Contracts.Admin;

public sealed record AdminUserListResponse(
    IReadOnlyList<AdminUserResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
