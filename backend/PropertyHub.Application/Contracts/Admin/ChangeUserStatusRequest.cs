using PropertyHub.Domain.Enums;

namespace PropertyHub.Application.Contracts.Admin;

public sealed record ChangeUserStatusRequest(AccountStatus Status, string? Reason);
