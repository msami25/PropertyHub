using System.Net;
using FluentAssertions;
using PropertyHub.IntegrationTests.Infrastructure;

namespace PropertyHub.IntegrationTests.Api;

public sealed class HealthEndpointTests(PropertyHubWebApplicationFactory factory)
    : IClassFixture<PropertyHubWebApplicationFactory>
{
    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpoint_ShouldReturnHealthy(string path)
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
