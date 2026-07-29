using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PropertyHub.Infrastructure.Data;
using PropertyHub.Application.Interfaces.Services;
using PropertyHub.Application.Models.Weather;

namespace PropertyHub.IntegrationTests.Infrastructure;

public sealed class PropertyHubWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"PropertyHubTests-{Guid.NewGuid()}";
    private readonly string _imageRoot = Path.Combine(
        Path.GetTempPath(),
        $"PropertyHubImages-{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        });
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "PropertyHub.Tests",
                ["Jwt:Audience"] = "PropertyHub.Tests.Client",
                ["Jwt:SigningKey"] = "propertyhub-integration-tests-signing-key-2026",
                ["Jwt:AccessTokenMinutes"] = "30",
                ["SeedAdmin:Email"] = "admin@propertyhub.test",
                ["SeedAdmin:Password"] = "TestingAdmin!123",
                ["SeedAdmin:FullName"] = "Test Administrator",
                ["ImageStorage:RootPath"] = _imageRoot
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(
                options => options.UseInMemoryDatabase(_databaseName));
            services.RemoveAll<IOpenMeteoWeatherClient>();
            services.AddSingleton<IOpenMeteoWeatherClient, UnavailableWeatherClient>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_imageRoot))
        {
            Directory.Delete(_imageRoot, recursive: true);
        }
    }

    private sealed class UnavailableWeatherClient : IOpenMeteoWeatherClient
    {
        public Task<CurrentWeather?> GetCurrentAsync(
            Guid cityId,
            decimal latitude,
            decimal longitude,
            CancellationToken cancellationToken) =>
            Task.FromResult<CurrentWeather?>(null);
    }
}
