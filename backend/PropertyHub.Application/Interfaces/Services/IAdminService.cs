using PropertyHub.Application.Contracts.Admin;
using PropertyHub.Application.Models.Admin;

namespace PropertyHub.Application.Interfaces.Services;

public interface IAdminService
{
    Task<AdminDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken);

    Task<AdminUserListResult> ListUsersAsync(
        AdminUserQueryRequest request,
        CancellationToken cancellationToken);

    Task<AdminUserMutationResult> ChangeRoleAsync(
        Guid actorUserId,
        Guid targetUserId,
        string? expectedVersion,
        ChangeUserRoleRequest request,
        CancellationToken cancellationToken);

    Task<AdminUserMutationResult> ChangeStatusAsync(
        Guid actorUserId,
        Guid targetUserId,
        string? expectedVersion,
        ChangeUserStatusRequest request,
        string correlationId,
        CancellationToken cancellationToken);
}
