using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PropertyHub.Application.Contracts.Auth;
using PropertyHub.Application.Contracts.Properties;
using PropertyHub.Domain.Enums;
using PropertyHub.IntegrationTests.Infrastructure;
using PropertyHub.Infrastructure.Data;
using PropertyHub.Infrastructure.Identity;

namespace PropertyHub.IntegrationTests.Api;

public sealed class PropertyEndpointTests(PropertyHubWebApplicationFactory factory)
    : IClassFixture<PropertyHubWebApplicationFactory>
{
    private static readonly Guid LahoreId = Guid.Parse("10000000-0000-4000-8000-000000000001");
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [Fact]
    public async Task Owner_ShouldCompletePropertyCrud()
    {
        using var client = factory.CreateClient();
        await RegisterAndAuthenticateAsync(client, $"owner-{Guid.NewGuid():N}@propertyhub.test");

        var create = await client.PostAsJsonAsync("/api/properties", ValidCreate());
        create.StatusCode.Should().Be(HttpStatusCode.Created, await create.Content.ReadAsStringAsync());
        var property = await ReadAsync<PropertyManagementResponse>(create);
        property.ModerationStatus.Should().Be(ModerationStatus.Pending);
        property.AvailabilityStatus.Should().Be(AvailabilityStatus.Available);

        var owned = await client.GetFromJsonAsync<PropertyManagementListResponse>(
            "/api/users/me/properties",
            JsonOptions);
        owned!.Items.Should().Contain(item => item.Id == property.Id);

        var update = await client.PutAsJsonAsync(
            $"/api/properties/{property.Id}",
            ValidUpdate() with { Title = "Updated family property" });
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadAsync<PropertyManagementResponse>(update)).Title
            .Should().Be("Updated family property");

        var delete = await client.DeleteAsync($"/api/properties/{property.Id}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var missing = await client.GetAsync($"/api/users/me/properties/{property.Id}");
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PropertyMutations_ShouldReturn401AndHideOtherOwnersResources()
    {
        using var client = factory.CreateClient();
        var missingToken = await client.PostAsJsonAsync("/api/properties", ValidCreate());

        await RegisterAndAuthenticateAsync(client, $"first-{Guid.NewGuid():N}@propertyhub.test");
        var create = await client.PostAsJsonAsync("/api/properties", ValidCreate());
        var property = await ReadAsync<PropertyManagementResponse>(create);
        await UploadImageAsync(client, property.Id);

        await RegisterAndAuthenticateAsync(client, $"second-{Guid.NewGuid():N}@propertyhub.test");
        var crossRead = await client.GetAsync($"/api/users/me/properties/{property.Id}");
        var crossUpdate = await client.PutAsJsonAsync(
            $"/api/properties/{property.Id}",
            ValidUpdate() with { Title = "Attempted cross-user edit" });
        var crossDelete = await client.DeleteAsync($"/api/properties/{property.Id}");

        missingToken.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        crossRead.StatusCode.Should().Be(HttpStatusCode.NotFound);
        crossUpdate.StatusCode.Should().Be(HttpStatusCode.NotFound);
        crossDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ShouldValidateRulesCityAndDuplicateSignature()
    {
        using var client = factory.CreateClient();
        await RegisterAndAuthenticateAsync(client, $"validation-{Guid.NewGuid():N}@propertyhub.test");

        var invalidRooms = await client.PostAsJsonAsync(
            "/api/properties",
            ValidCreate() with { Bedrooms = null });
        var inactiveCity = await client.PostAsJsonAsync(
            "/api/properties",
            ValidCreate() with { CityId = Guid.NewGuid() });
        var first = await client.PostAsJsonAsync("/api/properties", ValidCreate());
        var duplicate = await client.PostAsJsonAsync(
            "/api/properties",
            ValidCreate() with { Title = " family property ", Address = " dha phase 6 " });

        invalidRooms.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        inactiveCity.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Moderation_ShouldControlPublicVisibilityAndRequireAdmin()
    {
        using var client = factory.CreateClient();
        await RegisterAndAuthenticateAsync(client, $"moderation-{Guid.NewGuid():N}@propertyhub.test");
        var create = await client.PostAsJsonAsync("/api/properties", ValidCreate());
        var property = await ReadAsync<PropertyManagementResponse>(create);
        await UploadImageAsync(client, property.Id);

        client.DefaultRequestHeaders.Authorization = null;
        var hiddenBeforeApproval = await client.GetAsync($"/api/properties/{property.Id}");
        var missingAdminToken = await client.PostAsJsonAsync(
            $"/api/admin/properties/{property.Id}/moderation",
            new ModeratePropertyRequest(ModerationStatus.Approved, null));

        await RegisterAndAuthenticateAsync(client, $"nonadmin-{Guid.NewGuid():N}@propertyhub.test");
        var userModeration = await client.PostAsJsonAsync(
            $"/api/admin/properties/{property.Id}/moderation",
            new ModeratePropertyRequest(ModerationStatus.Approved, null));

        await AuthenticateAsync(client, "admin@propertyhub.test", "TestingAdmin!123");
        var approve = await client.PostAsJsonAsync(
            $"/api/admin/properties/{property.Id}/moderation",
            new ModeratePropertyRequest(ModerationStatus.Approved, null));
        approve.StatusCode.Should().Be(HttpStatusCode.OK);

        client.DefaultRequestHeaders.Authorization = null;
        var publicDetail = await client.GetAsync($"/api/properties/{property.Id}");
        var publicList = await client.GetFromJsonAsync<PropertyListResponse>(
            $"/api/properties?cityId={LahoreId}&purpose=Sale&propertyType=House",
            JsonOptions);

        hiddenBeforeApproval.StatusCode.Should().Be(HttpStatusCode.NotFound);
        missingAdminToken.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        userModeration.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        publicDetail.StatusCode.Should().Be(HttpStatusCode.OK);
        publicList!.Items.Should().Contain(item => item.Id == property.Id);
    }

    [Fact]
    public async Task Owner_ShouldMarkPropertySoldAndRemoveItFromPublicResults()
    {
        using var client = factory.CreateClient();
        var ownerEmail = $"sold-{Guid.NewGuid():N}@propertyhub.test";
        await RegisterAndAuthenticateAsync(client, ownerEmail);
        var create = await client.PostAsJsonAsync("/api/properties", ValidCreate());
        var property = await ReadAsync<PropertyManagementResponse>(create);
        await UploadImageAsync(client, property.Id);

        await AuthenticateAsync(client, "admin@propertyhub.test", "TestingAdmin!123");
        await client.PostAsJsonAsync(
            $"/api/admin/properties/{property.Id}/moderation",
            new ModeratePropertyRequest(ModerationStatus.Approved, null));
        await AuthenticateAsync(client, ownerEmail, "StrongPass!123");

        var wrongStatus = await client.PatchAsJsonAsync(
            $"/api/properties/{property.Id}/availability",
            new UpdateAvailabilityRequest(AvailabilityStatus.Rented));
        var sold = await client.PatchAsJsonAsync(
            $"/api/properties/{property.Id}/availability",
            new UpdateAvailabilityRequest(AvailabilityStatus.Sold));
        client.DefaultRequestHeaders.Authorization = null;
        var hidden = await client.GetAsync($"/api/properties/{property.Id}");

        wrongStatus.StatusCode.Should().Be(HttpStatusCode.Conflict);
        sold.StatusCode.Should().Be(HttpStatusCode.OK);
        hidden.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Moderation_ShouldRequireRejectionReasonAndHideDisabledOwner()
    {
        using var client = factory.CreateClient();
        var registration = await RegisterAndAuthenticateAsync(
            client,
            $"disabled-owner-{Guid.NewGuid():N}@propertyhub.test");
        var create = await client.PostAsJsonAsync("/api/properties", ValidCreate());
        var property = await ReadAsync<PropertyManagementResponse>(create);
        await UploadImageAsync(client, property.Id);

        await AuthenticateAsync(client, "admin@propertyhub.test", "TestingAdmin!123");
        var invalidReject = await client.PostAsJsonAsync(
            $"/api/admin/properties/{property.Id}/moderation",
            new ModeratePropertyRequest(ModerationStatus.Rejected, " "));
        var approve = await client.PostAsJsonAsync(
            $"/api/admin/properties/{property.Id}/moderation",
            new ModeratePropertyRequest(ModerationStatus.Approved, null));

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await context.Set<ApplicationUser>().SingleAsync(item => item.Id == registration.Id);
            user.Status = AccountStatus.Disabled;
            user.TokenVersion++;
            await context.SaveChangesAsync();
        }
        client.DefaultRequestHeaders.Authorization = null;
        var hidden = await client.GetAsync($"/api/properties/{property.Id}");

        invalidReject.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        approve.StatusCode.Should().Be(HttpStatusCode.OK);
        hidden.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static CreatePropertyRequest ValidCreate() =>
        new(
            "Family property",
            "A complete family property description.",
            PropertyPurpose.Sale,
            PropertyType.House,
            LahoreId,
            "DHA Phase 6",
            25_000_000,
            5,
            AreaUnit.Marla,
            3,
            3,
            "03000000000");

    private static UpdatePropertyRequest ValidUpdate() =>
        new(
            "Family property",
            "A complete updated property description.",
            PropertyPurpose.Sale,
            PropertyType.House,
            LahoreId,
            "DHA Phase 6",
            24_500_000,
            5,
            AreaUnit.Marla,
            3,
            3,
            "03000000000");

    private static async Task<RegistrationResponse> RegisterAndAuthenticateAsync(
        HttpClient client,
        string email)
    {
        client.DefaultRequestHeaders.Authorization = null;
        var registrationResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Property Owner", email, "StrongPass!123"));
        registrationResponse.EnsureSuccessStatusCode();
        var registration = await registrationResponse.Content.ReadFromJsonAsync<RegistrationResponse>();
        await AuthenticateAsync(client, email, "StrongPass!123");
        return registration!;
    }

    private static async Task AuthenticateAsync(HttpClient client, string email, string password)
    {
        client.DefaultRequestHeaders.Authorization = null;
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<AuthTokenResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token!.AccessToken);
    }

    private static async Task UploadImageAsync(HttpClient client, Guid propertyId)
    {
        using var content = new MultipartFormDataContent();
        using var image = new ByteArrayContent(
            [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00]);
        image.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(image, "images", "property.png");
        var response = await client.PostAsync($"/api/properties/{propertyId}/images", content);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
