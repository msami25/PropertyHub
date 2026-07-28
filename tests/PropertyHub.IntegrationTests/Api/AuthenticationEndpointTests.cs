using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PropertyHub.Application.Contracts.Auth;
using PropertyHub.Domain.Enums;
using PropertyHub.IntegrationTests.Infrastructure;
using PropertyHub.Infrastructure.Data;
using PropertyHub.Infrastructure.Identity;

namespace PropertyHub.IntegrationTests.Api;

public sealed class AuthenticationEndpointTests(PropertyHubWebApplicationFactory factory)
    : IClassFixture<PropertyHubWebApplicationFactory>
{
    [Fact]
    public async Task RegisterAndLogin_ShouldIssueUserTokenAndAllowActiveEndpoint()
    {
        using var client = factory.CreateClient();
        var email = $"user-{Guid.NewGuid():N}@propertyhub.test";

        var registration = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Active User", email, "StrongPass!123"));

        registration.StatusCode.Should().Be(
            HttpStatusCode.Created,
            await registration.Content.ReadAsStringAsync());
        var registeredUser = await registration.Content.ReadFromJsonAsync<RegistrationResponse>();
        registeredUser.Should().NotBeNull();
        registeredUser!.Role.Should().Be("User");

        var token = await LoginAsync(client, email, "StrongPass!123");
        token.User.Role.Should().Be("User");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        var currentUser = await client.GetAsync("/api/auth/me");

        currentUser.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProtectedEndpoints_ShouldReturn401ForMissingOrInvalidToken()
    {
        using var client = factory.CreateClient();

        var missing = await client.GetAsync("/api/auth/me");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        var invalid = await client.GetAsync("/api/auth/me");

        missing.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        invalid.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminEndpoint_ShouldReturn403ForAuthenticatedUser()
    {
        using var client = factory.CreateClient();
        var email = $"non-admin-{Guid.NewGuid():N}@propertyhub.test";
        await RegisterAsync(client, email);
        var token = await LoginAsync(client, email, "StrongPass!123");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        var response = await client.GetAsync("/api/admin/session");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SeededAdmin_ShouldLoginAndAccessAdminEndpoint()
    {
        using var client = factory.CreateClient();
        var token = await LoginAsync(client, "admin@propertyhub.test", "TestingAdmin!123");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        var response = await client.GetAsync("/api/admin/session");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        token.User.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task DisabledAccount_ShouldReceive403ForLoginAndExistingToken()
    {
        using var client = factory.CreateClient();
        var email = $"disabled-{Guid.NewGuid():N}@propertyhub.test";
        var user = await RegisterAsync(client, email);
        var token = await LoginAsync(client, email, "StrongPass!123");

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var account = await context.Set<ApplicationUser>().SingleAsync(item => item.Id == user.Id);
            account.Status = AccountStatus.Disabled;
            account.TokenVersion++;
            await context.SaveChangesAsync();
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        var existingTokenResponse = await client.GetAsync("/api/auth/me");
        client.DefaultRequestHeaders.Authorization = null;
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, "StrongPass!123"));

        existingTokenResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task InvalidCredentials_ShouldReturnGeneric401()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest($"missing-{Guid.NewGuid():N}@propertyhub.test", "WrongPass!123"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).Should().NotContain("missing-");
    }

    [Fact]
    public async Task Registration_ShouldRejectClientSuppliedRole()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                fullName = "Role Escalation",
                email = $"role-{Guid.NewGuid():N}@propertyhub.test",
                password = "StrongPass!123",
                role = "Admin"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task<RegistrationResponse> RegisterAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Test User", email, "StrongPass!123"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RegistrationResponse>())!;
    }

    private static async Task<AuthTokenResponse> LoginAsync(
        HttpClient client,
        string email,
        string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthTokenResponse>())!;
    }
}
