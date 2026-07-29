using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PropertyHub.Application.Contracts.Admin;
using PropertyHub.Application.Contracts.Auth;
using PropertyHub.Application.Contracts.Properties;
using PropertyHub.Domain.Authorization;
using PropertyHub.Domain.Entities;
using PropertyHub.Domain.Enums;
using PropertyHub.IntegrationTests.Infrastructure;
using PropertyHub.Infrastructure.Data;
using PropertyHub.Infrastructure.Identity;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PropertyHub.IntegrationTests.Api;

public sealed class AdminEndpointTests(PropertyHubWebApplicationFactory factory)
    : IClassFixture<PropertyHubWebApplicationFactory>
{
    private static readonly Guid LahoreId = Guid.Parse("10000000-0000-4000-8000-000000000001");
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [Fact]
    public async Task AdminEndpoints_ShouldReturn401And403ForUnauthorizedActors()
    {
        using var client = factory.CreateClient();
        var missingDashboard = await client.GetAsync("/api/admin/dashboard");
        var missingUsers = await client.GetAsync("/api/admin/users");

        var email = $"admin-auth-{Guid.NewGuid():N}@propertyhub.test";
        await RegisterAsync(client, email);
        var userToken = await LoginAsync(client, email);
        SetToken(client, userToken);
        var forbiddenDashboard = await client.GetAsync("/api/admin/dashboard");
        var forbiddenUsers = await client.GetAsync("/api/admin/users");

        missingDashboard.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        missingUsers.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        forbiddenDashboard.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        forbiddenUsers.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Dashboard_ShouldReturnCurrentDatabaseMetrics()
    {
        using var client = factory.CreateClient();
        await AuthenticateAdminAsync(client);

        var response = await client.GetAsync("/api/admin/dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dashboard = await ReadAsync<AdminDashboardResponse>(response);

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var adminRoleId = await context.Roles
            .Where(role => role.Name == RoleNames.Admin)
            .Select(role => role.Id)
            .SingleAsync();
        var adminIds = context.UserRoles
            .Where(userRole => userRole.RoleId == adminRoleId)
            .Select(userRole => userRole.UserId);

        dashboard.Users.Total.Should().Be(await context.Users.CountAsync());
        dashboard.Users.Registered.Should().Be(
            await context.Users.CountAsync(user => !adminIds.Contains(user.Id)));
        dashboard.Users.Active.Should().Be(
            await context.Users.CountAsync(user => user.Status == AccountStatus.Active));
        dashboard.Users.Disabled.Should().Be(
            await context.Users.CountAsync(user => user.Status == AccountStatus.Disabled));
        dashboard.Properties.Total.Should().Be(
            await context.Properties.CountAsync(property => !property.IsDeleted));
        dashboard.Properties.Pending.Should().Be(
            await context.Properties.CountAsync(property =>
                !property.IsDeleted && property.ModerationStatus == ModerationStatus.Pending));
        dashboard.Properties.Approved.Should().Be(
            await context.Properties.CountAsync(property =>
                !property.IsDeleted && property.ModerationStatus == ModerationStatus.Approved));
        dashboard.Properties.Rejected.Should().Be(
            await context.Properties.CountAsync(property =>
                !property.IsDeleted && property.ModerationStatus == ModerationStatus.Rejected));
        dashboard.TotalCities.Should().Be(await context.Cities.CountAsync());
    }

    [Fact]
    public async Task Admin_ShouldPromoteAndDemoteUserWithImmediateTokenInvalidation()
    {
        using var client = factory.CreateClient();
        var email = $"role-change-{Guid.NewGuid():N}@propertyhub.test";
        var registered = await RegisterAsync(client, email);
        var originalUserToken = await LoginAsync(client, email);

        await AuthenticateAdminAsync(client);
        var user = await FindUserAsync(client, email);
        var promote = await PatchWithVersionAsync(
            client,
            $"/api/admin/users/{registered.Id}/role",
            user.Version,
            new ChangeUserRoleRequest(RoleNames.Admin));
        promote.StatusCode.Should().Be(HttpStatusCode.OK, await promote.Content.ReadAsStringAsync());
        (await ReadAsync<AdminUserResponse>(promote)).Role.Should().Be(RoleNames.Admin);

        SetToken(client, originalUserToken);
        (await client.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var promotedToken = await LoginAsync(client, email);
        promotedToken.User.Role.Should().Be(RoleNames.Admin);

        SetToken(client, promotedToken);
        var ownDemotionUser = await FindUserAsync(client, email);
        var ownDemotion = await PatchWithVersionAsync(
            client,
            $"/api/admin/users/{registered.Id}/role",
            ownDemotionUser.Version,
            new ChangeUserRoleRequest(RoleNames.User));
        ownDemotion.StatusCode.Should().Be(HttpStatusCode.Conflict);

        await AuthenticateAdminAsync(client);
        var promotedUser = await FindUserAsync(client, email);
        var demote = await PatchWithVersionAsync(
            client,
            $"/api/admin/users/{registered.Id}/role",
            promotedUser.Version,
            new ChangeUserRoleRequest(RoleNames.User));
        demote.StatusCode.Should().Be(HttpStatusCode.OK, await demote.Content.ReadAsStringAsync());

        SetToken(client, promotedToken);
        (await client.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await LoginAsync(client, email)).User.Role.Should().Be(RoleNames.User);
    }

    [Fact]
    public async Task Admin_ShouldDisableAndEnableUserWithAuditAndVisibilityChanges()
    {
        using var client = factory.CreateClient();
        var email = $"status-change-{Guid.NewGuid():N}@propertyhub.test";
        var registered = await RegisterAsync(client, email);
        var ownerToken = await LoginAsync(client, email);
        SetToken(client, ownerToken);
        var property = await CreateApprovedPropertyAsync(client);

        await AuthenticateAdminAsync(client);
        var user = await FindUserAsync(client, email);
        var disable = await PatchWithVersionAsync(
            client,
            $"/api/admin/users/{registered.Id}/status",
            user.Version,
            new ChangeUserStatusRequest(
                AccountStatus.Disabled,
                "Repeated unsafe property submissions"));
        disable.StatusCode.Should().Be(HttpStatusCode.OK, await disable.Content.ReadAsStringAsync());
        (await ReadAsync<AdminUserResponse>(disable)).Status.Should().Be(AccountStatus.Disabled);

        SetToken(client, ownerToken);
        (await client.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        client.DefaultRequestHeaders.Authorization = null;
        (await client.GetAsync($"/api/properties/{property.Id}")).StatusCode
            .Should().Be(HttpStatusCode.NotFound);
        (await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, "StrongPass!123"))).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var audit = await context.UserStatusChanges
                .SingleAsync(change => change.TargetUserId == registered.Id);
            audit.PreviousStatus.Should().Be(AccountStatus.Active);
            audit.NewStatus.Should().Be(AccountStatus.Disabled);
            audit.Reason.Should().Be("Repeated unsafe property submissions");
            audit.CorrelationId.Should().NotBeNullOrWhiteSpace();
        }

        await AuthenticateAdminAsync(client);
        var disabledUser = await FindUserAsync(client, email);
        var enable = await PatchWithVersionAsync(
            client,
            $"/api/admin/users/{registered.Id}/status",
            disabledUser.Version,
            new ChangeUserStatusRequest(AccountStatus.Active, "Administrative review completed"));
        enable.StatusCode.Should().Be(HttpStatusCode.OK, await enable.Content.ReadAsStringAsync());
        (await LoginAsync(client, email)).User.Role.Should().Be(RoleNames.User);
        client.DefaultRequestHeaders.Authorization = null;
        (await client.GetAsync($"/api/properties/{property.Id}")).StatusCode
            .Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminMutations_ShouldValidateInputVersionAndUnsafeSelfActions()
    {
        using var client = factory.CreateClient();
        var adminToken = await AuthenticateAdminAsync(client);
        var admin = await FindUserAsync(client, "admin@propertyhub.test");

        var selfDemotion = await PatchWithVersionAsync(
            client,
            $"/api/admin/users/{admin.Id}/role",
            admin.Version,
            new ChangeUserRoleRequest(RoleNames.User));
        var selfDisable = await PatchWithVersionAsync(
            client,
            $"/api/admin/users/{admin.Id}/status",
            admin.Version,
            new ChangeUserStatusRequest(AccountStatus.Disabled, "Unsafe self disable attempt"));

        var email = $"validation-{Guid.NewGuid():N}@propertyhub.test";
        var registered = await RegisterAsync(client, email);
        SetToken(client, adminToken);
        var user = await FindUserAsync(client, email);
        var invalidRole = await PatchWithVersionAsync(
            client,
            $"/api/admin/users/{registered.Id}/role",
            user.Version,
            new ChangeUserRoleRequest("Owner"));
        var shortReason = await PatchWithVersionAsync(
            client,
            $"/api/admin/users/{registered.Id}/status",
            user.Version,
            new ChangeUserStatusRequest(AccountStatus.Disabled, "bad"));
        var stale = await PatchWithVersionAsync(
            client,
            $"/api/admin/users/{registered.Id}/status",
            "stale",
            new ChangeUserStatusRequest(AccountStatus.Disabled, "Valid administrative reason"));
        var invalidPage = await client.GetAsync("/api/admin/users?page=0&pageSize=51");

        selfDemotion.StatusCode.Should().Be(HttpStatusCode.Conflict);
        selfDisable.StatusCode.Should().Be(HttpStatusCode.Conflict);
        invalidRole.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        shortReason.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        stale.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        invalidPage.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.GetAsync("/api/admin/session")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<PropertyManagementResponse> CreateApprovedPropertyAsync(HttpClient client)
    {
        var create = await client.PostAsJsonAsync(
            "/api/properties",
            new CreatePropertyRequest(
                "Admin status visibility property",
                "A complete property used for account status verification.",
                PropertyPurpose.Sale,
                PropertyType.House,
                LahoreId,
                "Status Test Avenue",
                20_000_000,
                5,
                AreaUnit.Marla,
                3,
                2,
                "03000000000"));
        create.EnsureSuccessStatusCode();
        var property = await ReadAsync<PropertyManagementResponse>(create);
        await UploadImageAsync(client, property.Id);
        await AuthenticateAdminAsync(client);
        var approve = await client.PostAsJsonAsync(
            $"/api/admin/properties/{property.Id}/moderation",
            new ModeratePropertyRequest(ModerationStatus.Approved, null));
        approve.EnsureSuccessStatusCode();
        return property;
    }

    private static async Task UploadImageAsync(HttpClient client, Guid propertyId)
    {
        using var source = new Image<Rgba32>(1, 1);
        using var stream = new MemoryStream();
        source.SaveAsPng(stream);
        using var image = new ByteArrayContent(stream.ToArray());
        image.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        using var content = new MultipartFormDataContent();
        content.Add(image, "images", "property.png");
        (await client.PostAsync($"/api/properties/{propertyId}/images", content))
            .EnsureSuccessStatusCode();
    }

    private static async Task<RegistrationResponse> RegisterAsync(HttpClient client, string email)
    {
        client.DefaultRequestHeaders.Authorization = null;
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Managed User", email, "StrongPass!123"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RegistrationResponse>())!;
    }

    private static async Task<AuthTokenResponse> AuthenticateAdminAsync(HttpClient client)
    {
        var token = await LoginAsync(client, "admin@propertyhub.test", "TestingAdmin!123");
        SetToken(client, token);
        return token;
    }

    private static async Task<AuthTokenResponse> LoginAsync(
        HttpClient client,
        string email,
        string password = "StrongPass!123")
    {
        client.DefaultRequestHeaders.Authorization = null;
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthTokenResponse>())!;
    }

    private static async Task<AdminUserResponse> FindUserAsync(HttpClient client, string search)
    {
        var response = await client.GetAsync(
            $"/api/admin/users?search={Uri.EscapeDataString(search)}");
        response.EnsureSuccessStatusCode();
        var users = await ReadAsync<AdminUserListResponse>(response);
        return users.Items.Should().ContainSingle().Subject;
    }

    private static Task<HttpResponseMessage> PatchWithVersionAsync(
        HttpClient client,
        string path,
        string version,
        object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, path)
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        request.Headers.TryAddWithoutValidation("If-Match", $"\"{version}\"");
        return client.SendAsync(request);
    }

    private static void SetToken(HttpClient client, AuthTokenResponse token) =>
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token.AccessToken);

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
