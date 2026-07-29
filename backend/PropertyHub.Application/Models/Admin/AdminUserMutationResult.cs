using PropertyHub.Application.Contracts.Admin;

namespace PropertyHub.Application.Models.Admin;

public sealed record AdminUserMutationResult(
    AdminUserMutationOutcome Outcome,
    AdminUserResponse? User = null,
    string? Error = null);

public enum AdminUserMutationOutcome
{
    Success,
    NotFound,
    Invalid,
    Conflict,
    VersionMismatch
}
