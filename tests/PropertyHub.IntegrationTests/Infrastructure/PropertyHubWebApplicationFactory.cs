using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PropertyHub.Infrastructure.Data;

namespace PropertyHub.IntegrationTests.Infrastructure;

public sealed class PropertyHubWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"PropertyHubTests-{Guid.NewGuid()}";

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
                ["SeedAdmin:FullName"] = "Test Administrator"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(
                options => options.UseInMemoryDatabase(_databaseName));
        });
    }
}
