using PropertyHub.Domain.Enums;

namespace PropertyHub.Application.Models.Admin;

public sealed record AdminUserAccount(
    Guid Id,
    string FullName,
    string Email,
    AccountStatus Status,
    int TokenVersion,
    IReadOnlyList<string> Roles,
    int PropertyCount,
    DateTime CreatedAtUtc);
