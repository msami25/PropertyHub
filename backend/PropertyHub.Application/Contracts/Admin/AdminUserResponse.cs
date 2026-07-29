using PropertyHub.Domain.Enums;

namespace PropertyHub.Application.Contracts.Admin;

public sealed record AdminUserResponse(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    AccountStatus Status,
    int PropertyCount,
    DateTime CreatedAtUtc,
    string Version);
