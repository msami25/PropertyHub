using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PropertyHub.Application.Interfaces.Repositories;
using PropertyHub.Application.Interfaces.Services;
using PropertyHub.Infrastructure.Data;
using PropertyHub.Infrastructure.Data.Repositories;
using PropertyHub.Infrastructure.Files;
using PropertyHub.Infrastructure.External;
using PropertyHub.Infrastructure.Identity;

namespace PropertyHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost;Database=PropertyHub;Trusted_Connection=True;TrustServerCertificate=True";

        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddSignInManager()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddScoped<IUserAccountRepository, IdentityUserAccountRepository>();
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
        services.AddScoped<ICityRepository, CityRepository>();
        services.AddScoped<IPropertyRepository, PropertyRepository>();
        services.AddScoped<IPropertyImageRepository, PropertyImageRepository>();
        services.AddMemoryCache();
        var openMeteoBaseUrl = configuration["OpenMeteo:BaseUrl"];
        if (string.IsNullOrWhiteSpace(openMeteoBaseUrl))
        {
            openMeteoBaseUrl = "https://api.open-meteo.com";
        }
        var configuredTimeout = int.TryParse(
            configuration["OpenMeteo:TimeoutSeconds"],
            out var timeoutValue)
            ? timeoutValue
            : 5;
        var configuredCacheMinutes = int.TryParse(
            configuration["OpenMeteo:CacheMinutes"],
            out var cacheValue)
            ? cacheValue
            : 30;
        var timeoutSeconds = Math.Clamp(
            configuredTimeout,
            1,
            30);
        var cacheMinutes = Math.Clamp(
            configuredCacheMinutes,
            1,
            1440);
        var openMeteoOptions = new OpenMeteoOptions(
            openMeteoBaseUrl,
            TimeSpan.FromSeconds(timeoutSeconds),
            TimeSpan.FromMinutes(cacheMinutes));
        services.AddSingleton(openMeteoOptions);
        services.AddHttpClient<IOpenMeteoWeatherClient, OpenMeteoWeatherClient>(client =>
        {
            client.BaseAddress = new Uri($"{openMeteoOptions.BaseUrl.TrimEnd('/')}/");
            client.Timeout = openMeteoOptions.Timeout;
        });
        var imageRoot = configuration["ImageStorage:RootPath"];
        if (string.IsNullOrWhiteSpace(imageRoot))
        {
            imageRoot = Path.Combine(AppContext.BaseDirectory, "uploads");
        }
        services.AddSingleton<IImageStorage>(new LocalImageStorage(imageRoot));
        return services;
    }
}
