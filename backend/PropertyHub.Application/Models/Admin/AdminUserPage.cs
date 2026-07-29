namespace PropertyHub.Application.Models.Admin;

public sealed record AdminUserPage(
    IReadOnlyList<AdminUserAccount> Items,
    int TotalCount);
