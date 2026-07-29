using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PropertyHub.Application.Interfaces.Repositories;
using PropertyHub.Application.Models.Admin;
using PropertyHub.Domain.Authorization;
using PropertyHub.Domain.Entities;
using PropertyHub.Domain.Enums;
using PropertyHub.Infrastructure.Identity;

namespace PropertyHub.Infrastructure.Data.Repositories;

public sealed class AdminUserRepository(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager) : IAdminUserRepository
{
    public async Task<AdminDashboardData> GetDashboardDataAsync(CancellationToken cancellationToken)
    {
        var adminRoleId = await context.Roles
            .Where(role => role.Name == RoleNames.Admin)
            .Select(role => (Guid?)role.Id)
            .SingleOrDefaultAsync(cancellationToken);
        var adminUserIds = adminRoleId.HasValue
            ? context.UserRoles
                .Where(userRole => userRole.RoleId == adminRoleId.Value)
                .Select(userRole => userRole.UserId)
            : Enumerable.Empty<Guid>().AsQueryable();
        var users = context.Users.AsNoTracking();
        var properties = context.Properties.AsNoTracking().Where(property => !property.IsDeleted);

        return new AdminDashboardData(
            await users.CountAsync(cancellationToken),
            await users.CountAsync(user => !adminUserIds.Contains(user.Id), cancellationToken),
            await users.CountAsync(user => user.Status == AccountStatus.Active, cancellationToken),
            await users.CountAsync(user => user.Status == AccountStatus.Disabled, cancellationToken),
            await properties.CountAsync(cancellationToken),
            await properties.CountAsync(
                property => property.ModerationStatus == ModerationStatus.Pending,
                cancellationToken),
            await properties.CountAsync(
                property => property.ModerationStatus == ModerationStatus.Approved,
                cancellationToken),
            await properties.CountAsync(
                property => property.ModerationStatus == ModerationStatus.Rejected,
                cancellationToken),
            await context.Cities.CountAsync(cancellationToken));
    }

    public async Task<AdminUserPage> ListUsersAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = context.Users.AsNoTracking();
        if (search is not null)
        {
            query = query.Where(user =>
                user.FullName.Contains(search)
                || (user.Email != null && user.Email.Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var users = await query
            .OrderByDescending(user => user.CreatedAtUtc)
            .ThenBy(user => user.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var userIds = users.Select(user => user.Id).ToArray();
        var roles = await GetRolesAsync(userIds, cancellationToken);
        var propertyCounts = await GetPropertyCountsAsync(userIds, cancellationToken);
        var items = users.Select(user => ToAccount(
            user,
            roles.GetValueOrDefault(user.Id, [RoleNames.User]),
            propertyCounts.GetValueOrDefault(user.Id))).ToArray();
        return new AdminUserPage(items, totalCount);
    }

    public async Task<AdminUserAccount?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await context.Users.AsNoTracking()
            .SingleOrDefaultAsync(account => account.Id == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var roles = await GetRolesAsync([userId], cancellationToken);
        var propertyCounts = await GetPropertyCountsAsync([userId], cancellationToken);
        return ToAccount(
            user,
            roles.GetValueOrDefault(userId, [RoleNames.User]),
            propertyCounts.GetValueOrDefault(userId));
    }

    public async Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken)
    {
        var adminRoleId = await context.Roles
            .Where(role => role.Name == RoleNames.Admin)
            .Select(role => role.Id)
            .SingleAsync(cancellationToken);
        return await context.Users.CountAsync(
            user => user.Status == AccountStatus.Active
                && context.UserRoles.Any(userRole =>
                    userRole.UserId == user.Id && userRole.RoleId == adminRoleId),
            cancellationToken);
    }

    public async Task<AdminUserAccount> ChangeRoleAsync(
        Guid userId,
        string role,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("The account no longer exists.");
        var currentRoles = await userManager.GetRolesAsync(user);
        var rolesToRemove = currentRoles
            .Where(currentRole => RoleNames.All.Contains(currentRole, StringComparer.Ordinal)
                && !string.Equals(currentRole, role, StringComparison.Ordinal))
            .ToArray();

        if (!currentRoles.Contains(role, StringComparer.Ordinal))
        {
            EnsureSucceeded(
                await userManager.AddToRoleAsync(user, role),
                "The requested role could not be assigned.");
        }
        if (rolesToRemove.Length > 0)
        {
            EnsureSucceeded(
                await userManager.RemoveFromRolesAsync(user, rolesToRemove),
                "The previous role could not be removed.");
        }

        user.TokenVersion++;
        user.UpdatedAtUtc = DateTime.UtcNow;
        EnsureSucceeded(
            await userManager.UpdateAsync(user),
            "The account role change could not be completed.");
        return await ToAccountAsync(user, cancellationToken);
    }

    public async Task<AdminUserAccount> ChangeStatusAsync(
        Guid userId,
        AccountStatus status,
        UserStatusChange statusChange,
        CancellationToken cancellationToken)
    {
        var user = await context.Users.SingleAsync(
            account => account.Id == userId,
            cancellationToken);
        user.Status = status;
        user.TokenVersion++;
        user.UpdatedAtUtc = statusChange.CreatedAtUtc;
        await context.UserStatusChanges.AddAsync(statusChange, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return await ToAccountAsync(user, cancellationToken);
    }

    private async Task<AdminUserAccount> ToAccountAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        var roles = await GetRolesAsync([user.Id], cancellationToken);
        var propertyCounts = await GetPropertyCountsAsync([user.Id], cancellationToken);
        return ToAccount(
            user,
            roles.GetValueOrDefault(user.Id, [RoleNames.User]),
            propertyCounts.GetValueOrDefault(user.Id));
    }

    private async Task<Dictionary<Guid, IReadOnlyList<string>>> GetRolesAsync(
        Guid[] userIds,
        CancellationToken cancellationToken)
    {
        var rows = await (
            from userRole in context.UserRoles
            join role in context.Roles on userRole.RoleId equals role.Id
            where userIds.Contains(userRole.UserId)
            select new { userRole.UserId, role.Name })
            .ToListAsync(cancellationToken);
        return rows.GroupBy(row => row.UserId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group
                    .Select(row => row.Name!)
                    .ToArray());
    }

    private async Task<Dictionary<Guid, int>> GetPropertyCountsAsync(
        Guid[] userIds,
        CancellationToken cancellationToken) =>
        await context.SellerProfiles
            .Where(profile => userIds.Contains(profile.UserId))
            .Select(profile => new
            {
                profile.UserId,
                Count = profile.Properties.Count(property => !property.IsDeleted)
            })
            .ToDictionaryAsync(item => item.UserId, item => item.Count, cancellationToken);

    private static AdminUserAccount ToAccount(
        ApplicationUser user,
        IReadOnlyList<string> roles,
        int propertyCount) =>
        new(
            user.Id,
            user.FullName,
            user.Email!,
            user.Status,
            user.TokenVersion,
            roles,
            propertyCount,
            user.CreatedAtUtc);

    private static void EnsureSucceeded(IdentityResult result, string message)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errorCodes = string.Join(", ", result.Errors.Select(error => error.Code));
        throw new InvalidOperationException($"{message} Identity error codes: {errorCodes}");
    }
}
