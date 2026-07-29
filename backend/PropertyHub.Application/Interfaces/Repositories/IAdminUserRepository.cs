using PropertyHub.Application.Models.Admin;
using PropertyHub.Domain.Entities;
using PropertyHub.Domain.Enums;

namespace PropertyHub.Application.Interfaces.Repositories;

public interface IAdminUserRepository
{
    Task<AdminDashboardData> GetDashboardDataAsync(CancellationToken cancellationToken);

    Task<AdminUserPage> ListUsersAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<AdminUserAccount?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken);

    Task<AdminUserAccount> ChangeRoleAsync(
        Guid userId,
        string role,
        CancellationToken cancellationToken);

    Task<AdminUserAccount> ChangeStatusAsync(
        Guid userId,
        AccountStatus status,
        UserStatusChange statusChange,
        CancellationToken cancellationToken);
}
