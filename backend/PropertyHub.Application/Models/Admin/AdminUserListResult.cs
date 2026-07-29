using PropertyHub.Application.Contracts.Admin;

namespace PropertyHub.Application.Models.Admin;

public sealed record AdminUserListResult(
    bool Succeeded,
    AdminUserListResponse? Response = null,
    string? Error = null);
