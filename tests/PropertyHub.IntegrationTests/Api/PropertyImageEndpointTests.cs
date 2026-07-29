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
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PropertyHub.IntegrationTests.Api;

public sealed class PropertyImageEndpointTests(PropertyHubWebApplicationFactory factory)
    : IClassFixture<PropertyHubWebApplicationFactory>
{
    private static readonly Guid LahoreId = Guid.Parse("10000000-0000-4000-8000-000000000001");
    private static readonly byte[] PngBytes = CreatePng();
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [Fact]
    public async Task Owner_ShouldUploadManageAndServeImagesAcrossModeration()
    {
        using var client = factory.CreateClient();
        var ownerEmail = $"images-{Guid.NewGuid():N}@propertyhub.test";
        await RegisterAndAuthenticateAsync(client, ownerEmail);
        var property = await CreatePropertyAsync(client);

        var upload = await UploadAsync(
            client,
            property.Id,
            ("front.png", "image/png", PngBytes),
            ("back.png", "image/png", PngBytes));
        upload.StatusCode.Should().Be(HttpStatusCode.OK);
        var uploaded = await ReadAsync<PropertyImagesResponse>(upload);
        uploaded.Images.Should().HaveCount(2);
        uploaded.Images.Should().ContainSingle(image => image.IsPrimary);
        uploaded.ModerationStatus.Should().Be(ModerationStatus.Pending);

        var first = uploaded.Images[0];
        var second = uploaded.Images[1];
        var ownerImage = await client.GetAsync(first.Url);
        ownerImage.StatusCode.Should().Be(HttpStatusCode.OK);
        ownerImage.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
        (await ownerImage.Content.ReadAsByteArrayAsync()).Should().Equal(PngBytes);

        client.DefaultRequestHeaders.Authorization = null;
        (await client.GetAsync(first.Url)).StatusCode.Should().Be(HttpStatusCode.NotFound);

        await AuthenticateAsync(client, "admin@propertyhub.test", "TestingAdmin!123");
        var approve = await client.PostAsJsonAsync(
            $"/api/admin/properties/{property.Id}/moderation",
            new ModeratePropertyRequest(ModerationStatus.Approved, null));
        approve.StatusCode.Should().Be(HttpStatusCode.OK);

        client.DefaultRequestHeaders.Authorization = null;
        var publicImage = await client.GetAsync(first.Url);
        publicImage.StatusCode.Should().Be(HttpStatusCode.OK);
        publicImage.Headers.CacheControl!.Public.Should().BeTrue();
        publicImage.Headers.GetValues("X-Content-Type-Options").Should().ContainSingle("nosniff");

        await AuthenticateAsync(client, ownerEmail, "StrongPass!123");
        var primary = await client.PutAsync(
            $"/api/properties/{property.Id}/images/{second.Id}/primary",
            content: null);
        primary.StatusCode.Should().Be(HttpStatusCode.OK);
        var primaryResult = await ReadAsync<PropertyImagesResponse>(primary);
        primaryResult.ModerationStatus.Should().Be(ModerationStatus.Pending);
        primaryResult.Images.Single(image => image.Id == second.Id).IsPrimary.Should().BeTrue();

        var delete = await client.DeleteAsync(
            $"/api/properties/{property.Id}/images/{first.Id}");
        delete.StatusCode.Should().Be(HttpStatusCode.OK);
        var deleteLast = await client.DeleteAsync(
            $"/api/properties/{property.Id}/images/{second.Id}");
        deleteLast.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Upload_ShouldEnforceAuthenticationOwnershipValidationAndLimit()
    {
        using var client = factory.CreateClient();
        var anonymous = await UploadAsync(
            client,
            Guid.NewGuid(),
            ("property.png", "image/png", PngBytes));

        await RegisterAndAuthenticateAsync(client, $"first-images-{Guid.NewGuid():N}@propertyhub.test");
        var property = await CreatePropertyAsync(client);

        var invalidSignature = await UploadAsync(
            client,
            property.Id,
            ("property.jpg", "image/jpeg", [0x4D, 0x5A, 0x90, 0x00]));
        var five = await UploadAsync(
            client,
            property.Id,
            ("1.png", "image/png", PngBytes),
            ("2.png", "image/png", PngBytes),
            ("3.png", "image/png", PngBytes),
            ("4.png", "image/png", PngBytes),
            ("5.png", "image/png", PngBytes));
        var overLimit = await UploadAsync(
            client,
            property.Id,
            ("6.png", "image/png", PngBytes));

        await RegisterAndAuthenticateAsync(client, $"other-images-{Guid.NewGuid():N}@propertyhub.test");
        var otherOwner = await UploadAsync(
            client,
            property.Id,
            ("other.png", "image/png", PngBytes));

        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        invalidSignature.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        five.StatusCode.Should().Be(HttpStatusCode.OK);
        overLimit.StatusCode.Should().Be(HttpStatusCode.Conflict);
        otherOwner.StatusCode.Should().Be(HttpStatusCode.NotFound);

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stored = await context.PropertyImages
            .Where(image => image.PropertyId == property.Id)
            .ToListAsync();
        stored.Should().HaveCount(5);
        stored.Should().OnlyContain(image =>
            !image.RelativePath.Contains("..")
            && image.StoredFileName != image.OriginalFileName
            && image.Width == 1
            && image.Height == 1);
    }

    [Fact]
    public async Task Approval_ShouldFailWhenPropertyHasNoImage()
    {
        using var client = factory.CreateClient();
        await RegisterAndAuthenticateAsync(client, $"no-image-{Guid.NewGuid():N}@propertyhub.test");
        var property = await CreatePropertyAsync(client);

        await AuthenticateAsync(client, "admin@propertyhub.test", "TestingAdmin!123");
        var response = await client.PostAsJsonAsync(
            $"/api/admin/properties/{property.Id}/moderation",
            new ModeratePropertyRequest(ModerationStatus.Approved, null));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private static async Task<PropertyManagementResponse> CreatePropertyAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/properties",
            new CreatePropertyRequest(
                $"Image property {Guid.NewGuid():N}",
                "A complete property description for image testing.",
                PropertyPurpose.Sale,
                PropertyType.House,
                LahoreId,
                "DHA Phase 6",
                25_000_000,
                5,
                AreaUnit.Marla,
                3,
                3,
                "03000000000"));
        response.EnsureSuccessStatusCode();
        return await ReadAsync<PropertyManagementResponse>(response);
    }

    private static async Task<HttpResponseMessage> UploadAsync(
        HttpClient client,
        Guid propertyId,
        params (string FileName, string ContentType, byte[] Content)[] files)
    {
        using var form = new MultipartFormDataContent();
        foreach (var file in files)
        {
            var content = new ByteArrayContent(file.Content);
            content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            form.Add(content, "images", file.FileName);
        }
        return await client.PostAsync($"/api/properties/{propertyId}/images", form);
    }

    private static async Task RegisterAndAuthenticateAsync(HttpClient client, string email)
    {
        client.DefaultRequestHeaders.Authorization = null;
        var registration = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Property Owner", email, "StrongPass!123"));
        registration.EnsureSuccessStatusCode();
        await AuthenticateAsync(client, email, "StrongPass!123");
    }

    private static async Task AuthenticateAsync(HttpClient client, string email, string password)
    {
        client.DefaultRequestHeaders.Authorization = null;
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<AuthTokenResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token!.AccessToken);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static byte[] CreatePng()
    {
        using var image = new Image<Rgba32>(1, 1);
        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return stream.ToArray();
    }
}
