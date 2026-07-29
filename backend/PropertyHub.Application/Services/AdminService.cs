using PropertyHub.Application.Contracts.Admin;
using PropertyHub.Application.Interfaces.Repositories;
using PropertyHub.Application.Interfaces.Services;
using PropertyHub.Application.Models.Admin;
using PropertyHub.Domain.Authorization;
using PropertyHub.Domain.Entities;
using PropertyHub.Domain.Enums;

namespace PropertyHub.Application.Services;

public sealed class AdminService(
    IAdminUserRepository adminUserRepository,
    TimeProvider timeProvider) : IAdminService
{
    public async Task<AdminDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var data = await adminUserRepository.GetDashboardDataAsync(cancellationToken);
        return new AdminDashboardResponse(
            timeProvider.GetUtcNow().UtcDateTime,
            new AdminUserMetricsResponse(
                data.TotalUsers,
                data.RegisteredUsers,
                data.ActiveUsers,
                data.DisabledUsers),
            new AdminPropertyMetricsResponse(
                data.TotalProperties,
                data.PendingProperties,
                data.ApprovedProperties,
                data.RejectedProperties),
            data.TotalCities);
    }

    public async Task<AdminUserListResult> ListUsersAsync(
        AdminUserQueryRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Page < 1 || request.PageSize is < 1 or > 50)
        {
            return new AdminUserListResult(
                false,
                Error: "Page must be at least 1 and pageSize must be between 1 and 50.");
        }

        var search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        if (search?.Length > 100)
        {
            return new AdminUserListResult(false, Error: "Search must not exceed 100 characters.");
        }

        var page = await adminUserRepository.ListUsersAsync(
            search,
            request.Page,
            request.PageSize,
            cancellationToken);
        return new AdminUserListResult(
            true,
            new AdminUserListResponse(
                page.Items.Select(Map).ToArray(),
                request.Page,
                request.PageSize,
                page.TotalCount));
    }

    public async Task<AdminUserMutationResult> ChangeRoleAsync(
        Guid actorUserId,
        Guid targetUserId,
        string? expectedVersion,
        ChangeUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        var requestedRole = request.Role?.Trim() ?? string.Empty;
        if (!RoleNames.All.Contains(requestedRole, StringComparer.Ordinal))
        {
            return Invalid($"Role must be {RoleNames.User} or {RoleNames.Admin}.");
        }

        var target = await adminUserRepository.GetByIdAsync(targetUserId, cancellationToken);
        if (target is null)
        {
            return new AdminUserMutationResult(AdminUserMutationOutcome.NotFound);
        }

        var currentRole = GetRole(target);
        if (currentRole == requestedRole)
        {
            return new AdminUserMutationResult(AdminUserMutationOutcome.Success, Map(target));
        }

        if (!VersionMatches(target.TokenVersion, expectedVersion))
        {
            return new AdminUserMutationResult(
                AdminUserMutationOutcome.VersionMismatch,
                Error: "The account changed after it was loaded. Refresh and try again.");
        }

        if (actorUserId == targetUserId && currentRole == RoleNames.Admin)
        {
            return Conflict("Administrators cannot demote their own account.");
        }

        if (currentRole == RoleNames.Admin
            && requestedRole == RoleNames.User
            && target.Status == AccountStatus.Active
            && await adminUserRepository.CountActiveAdminsAsync(cancellationToken) <= 1)
        {
            return Conflict("The last active administrator cannot be demoted.");
        }

        var updated = await adminUserRepository.ChangeRoleAsync(
            targetUserId,
            requestedRole,
            cancellationToken);
        return new AdminUserMutationResult(AdminUserMutationOutcome.Success, Map(updated));
    }

    public async Task<AdminUserMutationResult> ChangeStatusAsync(
        Guid actorUserId,
        Guid targetUserId,
        string? expectedVersion,
        ChangeUserStatusRequest request,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var reason = request.Reason?.Trim() ?? string.Empty;
        if (reason.Length is < 5 or > 500)
        {
            return Invalid("Reason must contain 5 to 500 characters.");
        }

        var target = await adminUserRepository.GetByIdAsync(targetUserId, cancellationToken);
        if (target is null)
        {
            return new AdminUserMutationResult(AdminUserMutationOutcome.NotFound);
        }

        if (!VersionMatches(target.TokenVersion, expectedVersion))
        {
            return new AdminUserMutationResult(
                AdminUserMutationOutcome.VersionMismatch,
                Error: "The account changed after it was loaded. Refresh and try again.");
        }

        if (target.Status == request.Status)
        {
            return Conflict($"The account is already {request.Status}.");
        }

        if (actorUserId == targetUserId)
        {
            return Conflict("Administrators cannot disable their own account.");
        }

        if (GetRole(target) == RoleNames.Admin)
        {
            return Conflict("Administrator accounts cannot be disabled through user management.");
        }

        var statusChange = new UserStatusChange
        {
            TargetUserId = targetUserId,
            AdminUserId = actorUserId,
            PreviousStatus = target.Status,
            NewStatus = request.Status,
            Reason = reason,
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime,
            CorrelationId = correlationId
        };
        var updated = await adminUserRepository.ChangeStatusAsync(
            targetUserId,
            request.Status,
            statusChange,
            cancellationToken);
        return new AdminUserMutationResult(AdminUserMutationOutcome.Success, Map(updated));
    }

    private static AdminUserResponse Map(AdminUserAccount user) =>
        new(
            user.Id,
            user.FullName,
            user.Email,
            GetRole(user),
            user.Status,
            user.PropertyCount,
            user.CreatedAtUtc,
            EncodeVersion(user.TokenVersion));

    private static string GetRole(AdminUserAccount user) =>
        user.Roles.Contains(RoleNames.Admin, StringComparer.Ordinal)
            ? RoleNames.Admin
            : RoleNames.User;

    private static string EncodeVersion(int tokenVersion) =>
        Convert.ToBase64String(BitConverter.GetBytes(tokenVersion));

    private static bool VersionMatches(int tokenVersion, string? expectedVersion) =>
        !string.IsNullOrWhiteSpace(expectedVersion)
        && string.Equals(
            expectedVersion.Trim().Trim('"'),
            EncodeVersion(tokenVersion),
            StringComparison.Ordinal);

    private static AdminUserMutationResult Invalid(string error) =>
        new(AdminUserMutationOutcome.Invalid, Error: error);

    private static AdminUserMutationResult Conflict(string error) =>
        new(AdminUserMutationOutcome.Conflict, Error: error);
}
