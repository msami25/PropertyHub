using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PropertyHub.Infrastructure.Data;

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
        return services;
    }
}
