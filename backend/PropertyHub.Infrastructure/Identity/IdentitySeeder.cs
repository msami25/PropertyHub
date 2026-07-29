using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PropertyHub.Domain.Authorization;
using PropertyHub.Domain.Enums;
using PropertyHub.Infrastructure.Data;

namespace PropertyHub.Infrastructure.Identity;

public static class IdentitySeeder
{
    public static async Task InitialiseDatabaseAsync(
        this IServiceProvider serviceProvider,
        IHostEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ApplicationDbContext>();

        if (environment.IsEnvironment("Testing"))
        {
            await context.Database.EnsureCreatedAsync(cancellationToken);
        }
        else
        {
            await context.Database.MigrateAsync(cancellationToken);
        }

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var roleName in RoleNames.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                EnsureSucceeded(roleResult, $"Could not seed the {roleName} role.");
            }
        }

        var configuration = services.GetRequiredService<IConfiguration>();
        var adminEmail = configuration["SeedAdmin:Email"];
        var adminPassword = configuration["SeedAdmin:Password"];
        var adminFullName = configuration["SeedAdmin:FullName"] ?? "PropertyHub Administrator";
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder");

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("Seed administrator credentials are not configured; only roles were seeded.");
            return;
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FullName = adminFullName,
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                Status = AccountStatus.Active
            };

            var createResult = await userManager.CreateAsync(admin, adminPassword);
            EnsureSucceeded(createResult, "Could not seed the administrator account.");
        }

        if (!await userManager.IsInRoleAsync(admin, RoleNames.Admin))
        {
            var roleResult = await userManager.AddToRoleAsync(admin, RoleNames.Admin);
            EnsureSucceeded(roleResult, "Could not assign the administrator role.");
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string message)
    {
        if (result.Succeeded)
        {
            return;
        }

        var codes = string.Join(", ", result.Errors.Select(error => error.Code));
        throw new InvalidOperationException($"{message} Identity error codes: {codes}");
    }
}
