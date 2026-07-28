using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PropertyHub.Application.Contracts.Auth;
using PropertyHub.Application.Contracts.Cities;
using PropertyHub.Domain.Entities;
using PropertyHub.IntegrationTests.Infrastructure;
using PropertyHub.Infrastructure.Data;

namespace PropertyHub.IntegrationTests.Api;

public sealed class CityEndpointTests(PropertyHubWebApplicationFactory factory)
    : IClassFixture<PropertyHubWebApplicationFactory>
{
    [Fact]
    public async Task PublicList_ShouldReturnOnlyActiveCities()
    {
        using var client = factory.CreateClient();
        await AuthenticateAdminAsync(client);
        var inactiveName = $"Inactive-{Guid.NewGuid():N}";
        var createResponse = await client.PostAsJsonAsync(
            "/api/admin/cities",
            new CreateCityRequest(inactiveName, 30, 70, false));
        createResponse.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = null;

        var response = await client.GetAsync("/api/cities");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cities = await response.Content.ReadFromJsonAsync<CityListResponse>();
        cities!.Items.Should().HaveCountGreaterThanOrEqualTo(8);
        cities.Items.Should().OnlyContain(city => city.IsActive);
        cities.Items.Should().NotContain(city => city.Name == inactiveName);
    }

    [Fact]
    public async Task AdminCities_ShouldReturn401And403ForUnauthorizedActors()
    {
        using var client = factory.CreateClient();

        var missingToken = await client.GetAsync("/api/admin/cities");
        var userEmail = $"city-user-{Guid.NewGuid():N}@propertyhub.test";
        await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("City User", userEmail, "StrongPass!123"));
        await AuthenticateAsync(client, userEmail, "StrongPass!123");
        var nonAdmin = await client.GetAsync("/api/admin/cities");

        missingToken.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        nonAdmin.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_ShouldCompleteCityCrud()
    {
        using var client = factory.CreateClient();
        await AuthenticateAdminAsync(client);
        var originalName = $"Sialkot-{Guid.NewGuid():N}";

        var createResponse = await client.PostAsJsonAsync(
            "/api/admin/cities",
            new CreateCityRequest($"  {originalName}  ", 32.494500m, 74.522900m));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CityResponse>();
        created!.Name.Should().Be(originalName);
        createResponse.Headers.Location.Should().NotBeNull();

        var getResponse = await client.GetAsync($"/api/admin/cities/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/admin/cities/{created.Id}",
            new UpdateCityRequest($"{originalName}-Updated", 32.5m, 74.6m, false));
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<CityResponse>();
        updated!.IsActive.Should().BeFalse();
        updated.Name.Should().EndWith("-Updated");

        var listResponse = await client.GetFromJsonAsync<CityListResponse>("/api/admin/cities");
        listResponse!.Items.Should().Contain(city => city.Id == created.Id && !city.IsActive);

        var deleteResponse = await client.DeleteAsync($"/api/admin/cities/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var missingResponse = await client.GetAsync($"/api/admin/cities/{created.Id}");
        missingResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AdminMutation_ShouldValidateInputAndRejectDuplicateName()
    {
        using var client = factory.CreateClient();
        await AuthenticateAdminAsync(client);

        var invalid = await client.PostAsJsonAsync(
            "/api/admin/cities",
            new CreateCityRequest("Valid Name", 91, 0));
        var whitespace = await client.PostAsJsonAsync(
            "/api/admin/cities",
            new CreateCityRequest("  ", 31, 74));
        var duplicate = await client.PostAsJsonAsync(
            "/api/admin/cities",
            new CreateCityRequest(" lahore ", 31.520400m, 74.358700m));

        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        whitespace.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await duplicate.Content.ReadAsStringAsync()).Should().Contain("already exists");
    }

    [Fact]
    public async Task Delete_ShouldReturn409WhenPropertyReferencesCity()
    {
        using var client = factory.CreateClient();
        await AuthenticateAdminAsync(client);
        var createResponse = await client.PostAsJsonAsync(
            "/api/admin/cities",
            new CreateCityRequest($"Referenced-{Guid.NewGuid():N}", 30, 70));
        var city = await createResponse.Content.ReadFromJsonAsync<CityResponse>();

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Properties.Add(new Property
            {
                CityId = city!.Id,
                SellerProfileId = Guid.NewGuid(),
                Title = "Referenced property",
                Description = "Referenced property description",
                Address = "Referenced address",
                Price = 1,
                Area = 1,
                ContactNumber = "000"
            });
            await context.SaveChangesAsync();
        }

        var deleteResponse = await client.DeleteAsync($"/api/admin/cities/{city!.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await deleteResponse.Content.ReadAsStringAsync()).Should().Contain("properties use it");
        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        (await verificationContext.Cities.AnyAsync(item => item.Id == city.Id)).Should().BeTrue();
    }

    private static Task AuthenticateAdminAsync(HttpClient client) =>
        AuthenticateAsync(client, "admin@propertyhub.test", "TestingAdmin!123");

    private static async Task AuthenticateAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<AuthTokenResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token!.AccessToken);
    }
}
