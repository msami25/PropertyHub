using PropertyHub.Domain.Enums;

namespace PropertyHub.Application.Models.Auth;

public sealed record AccountSnapshot(
    Guid Id,
    string FullName,
    string Email,
    AccountStatus Status,
    int TokenVersion,
    IReadOnlyCollection<string> Roles);
