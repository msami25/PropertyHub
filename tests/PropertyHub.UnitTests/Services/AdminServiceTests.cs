using FluentAssertions;
using Moq;
using PropertyHub.Application.Contracts.Admin;
using PropertyHub.Application.Interfaces.Repositories;
using PropertyHub.Application.Models.Admin;
using PropertyHub.Application.Services;
using PropertyHub.Domain.Authorization;
using PropertyHub.Domain.Entities;
using PropertyHub.Domain.Enums;

namespace PropertyHub.UnitTests.Services;

public sealed class AdminServiceTests
{
    private readonly Mock<IAdminUserRepository> _repository = new();
    private readonly AdminService _service;

    public AdminServiceTests()
    {
        _service = new AdminService(_repository.Object, TimeProvider.System);
    }

    [Fact]
    public async Task GetDashboardAsync_ShouldMapLiveRepositoryCounts()
    {
        _repository.Setup(repository => repository.GetDashboardDataAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdminDashboardData(9, 8, 7, 2, 12, 3, 6, 3, 4));

        var response = await _service.GetDashboardAsync(CancellationToken.None);

        response.Users.Should().Be(new AdminUserMetricsResponse(9, 8, 7, 2));
        response.Properties.Should().Be(new AdminPropertyMetricsResponse(12, 3, 6, 3));
        response.TotalCities.Should().Be(4);
        response.AsOfUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task ListUsersAsync_ShouldValidatePaginationBeforeRepositoryAccess()
    {
        var result = await _service.ListUsersAsync(
            new AdminUserQueryRequest { Page = 0, PageSize = 51 },
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("pageSize");
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ChangeRoleAsync_ShouldRejectSelfDemotion()
    {
        var admin = CreateAccount(RoleNames.Admin);
        _repository.Setup(repository => repository.GetByIdAsync(
                admin.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);

        var result = await _service.ChangeRoleAsync(
            admin.Id,
            admin.Id,
            EncodeVersion(admin.TokenVersion),
            new ChangeUserRoleRequest(RoleNames.User),
            CancellationToken.None);

        result.Outcome.Should().Be(AdminUserMutationOutcome.Conflict);
        result.Error.Should().Contain("own account");
        _repository.Verify(repository => repository.ChangeRoleAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChangeRoleAsync_ShouldProtectLastActiveAdministrator()
    {
        var actor = Guid.NewGuid();
        var admin = CreateAccount(RoleNames.Admin);
        _repository.Setup(repository => repository.GetByIdAsync(
                admin.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);
        _repository.Setup(repository => repository.CountActiveAdminsAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _service.ChangeRoleAsync(
            actor,
            admin.Id,
            EncodeVersion(admin.TokenVersion),
            new ChangeUserRoleRequest(RoleNames.User),
            CancellationToken.None);

        result.Outcome.Should().Be(AdminUserMutationOutcome.Conflict);
        result.Error.Should().Contain("last active administrator");
    }

    [Fact]
    public async Task ChangeRoleAsync_ShouldPromoteUserAndReturnNewVersion()
    {
        var user = CreateAccount(RoleNames.User);
        var updated = user with { TokenVersion = user.TokenVersion + 1, Roles = [RoleNames.Admin] };
        _repository.Setup(repository => repository.GetByIdAsync(
                user.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _repository.Setup(repository => repository.ChangeRoleAsync(
                user.Id,
                RoleNames.Admin,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        var result = await _service.ChangeRoleAsync(
            Guid.NewGuid(),
            user.Id,
            EncodeVersion(user.TokenVersion),
            new ChangeUserRoleRequest(RoleNames.Admin),
            CancellationToken.None);

        result.Outcome.Should().Be(AdminUserMutationOutcome.Success);
        result.User!.Role.Should().Be(RoleNames.Admin);
        result.User.Version.Should().Be(EncodeVersion(updated.TokenVersion));
    }

    [Fact]
    public async Task ChangeStatusAsync_ShouldValidateReasonAndRejectAdminTargets()
    {
        var admin = CreateAccount(RoleNames.Admin);
        var invalid = await _service.ChangeStatusAsync(
            Guid.NewGuid(),
            admin.Id,
            EncodeVersion(admin.TokenVersion),
            new ChangeUserStatusRequest(AccountStatus.Disabled, "bad"),
            "trace",
            CancellationToken.None);
        invalid.Outcome.Should().Be(AdminUserMutationOutcome.Invalid);

        _repository.Setup(repository => repository.GetByIdAsync(
                admin.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);
        var unsafeResult = await _service.ChangeStatusAsync(
            Guid.NewGuid(),
            admin.Id,
            EncodeVersion(admin.TokenVersion),
            new ChangeUserStatusRequest(AccountStatus.Disabled, "Administrative review"),
            "trace",
            CancellationToken.None);

        unsafeResult.Outcome.Should().Be(AdminUserMutationOutcome.Conflict);
        unsafeResult.Error.Should().Contain("Administrator accounts");
    }

    [Fact]
    public async Task ChangeStatusAsync_ShouldPersistTrimmedAuditAndInvalidateTokenVersion()
    {
        var actorId = Guid.NewGuid();
        var user = CreateAccount(RoleNames.User);
        var updated = user with
        {
            Status = AccountStatus.Disabled,
            TokenVersion = user.TokenVersion + 1
        };
        UserStatusChange? capturedChange = null;
        _repository.Setup(repository => repository.GetByIdAsync(
                user.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _repository.Setup(repository => repository.ChangeStatusAsync(
                user.Id,
                AccountStatus.Disabled,
                It.IsAny<UserStatusChange>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, AccountStatus, UserStatusChange, CancellationToken>(
                (_, _, change, _) => capturedChange = change)
            .ReturnsAsync(updated);

        var result = await _service.ChangeStatusAsync(
            actorId,
            user.Id,
            EncodeVersion(user.TokenVersion),
            new ChangeUserStatusRequest(AccountStatus.Disabled, "  Repeated unsafe listings  "),
            "trace-id",
            CancellationToken.None);

        result.Outcome.Should().Be(AdminUserMutationOutcome.Success);
        result.User!.Status.Should().Be(AccountStatus.Disabled);
        capturedChange.Should().NotBeNull();
        capturedChange!.AdminUserId.Should().Be(actorId);
        capturedChange.TargetUserId.Should().Be(user.Id);
        capturedChange.PreviousStatus.Should().Be(AccountStatus.Active);
        capturedChange.NewStatus.Should().Be(AccountStatus.Disabled);
        capturedChange.Reason.Should().Be("Repeated unsafe listings");
        capturedChange.CorrelationId.Should().Be("trace-id");
    }

    [Fact]
    public async Task Mutations_ShouldRejectStaleOrMissingVersion()
    {
        var user = CreateAccount(RoleNames.User);
        _repository.Setup(repository => repository.GetByIdAsync(
                user.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var roleResult = await _service.ChangeRoleAsync(
            Guid.NewGuid(),
            user.Id,
            null,
            new ChangeUserRoleRequest(RoleNames.Admin),
            CancellationToken.None);
        var statusResult = await _service.ChangeStatusAsync(
            Guid.NewGuid(),
            user.Id,
            "\"stale\"",
            new ChangeUserStatusRequest(AccountStatus.Disabled, "Valid disable reason"),
            "trace",
            CancellationToken.None);

        roleResult.Outcome.Should().Be(AdminUserMutationOutcome.VersionMismatch);
        statusResult.Outcome.Should().Be(AdminUserMutationOutcome.VersionMismatch);
    }

    private static AdminUserAccount CreateAccount(string role) =>
        new(
            Guid.NewGuid(),
            "Managed User",
            "managed@propertyhub.test",
            AccountStatus.Active,
            2,
            [role],
            3,
            DateTime.UtcNow.AddDays(-1));

    private static string EncodeVersion(int tokenVersion) =>
        Convert.ToBase64String(BitConverter.GetBytes(tokenVersion));
}
