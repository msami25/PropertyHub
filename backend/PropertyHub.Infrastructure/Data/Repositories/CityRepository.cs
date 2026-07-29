using Microsoft.EntityFrameworkCore;
using PropertyHub.Application.Interfaces.Repositories;
using PropertyHub.Domain.Entities;

namespace PropertyHub.Infrastructure.Data.Repositories;

public sealed class CityRepository(ApplicationDbContext context) : ICityRepository
{
    public async Task<IReadOnlyList<City>> ListActiveAsync(CancellationToken cancellationToken) =>
        await context.Cities.AsNoTracking()
            .Where(city => city.IsActive)
            .OrderBy(city => city.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<City>> ListAllAsync(CancellationToken cancellationToken) =>
        await context.Cities.AsNoTracking()
            .OrderBy(city => city.Name)
            .ToListAsync(cancellationToken);

    public Task<City?> GetByIdAsync(Guid cityId, CancellationToken cancellationToken) =>
        context.Cities.AsNoTracking()
            .SingleOrDefaultAsync(city => city.Id == cityId, cancellationToken);

    public Task<City?> GetForUpdateAsync(Guid cityId, CancellationToken cancellationToken) =>
        context.Cities.SingleOrDefaultAsync(city => city.Id == cityId, cancellationToken);

    public Task<bool> NameExistsAsync(
        string normalizedName,
        Guid? excludedCityId,
        CancellationToken cancellationToken) =>
        context.Cities.AnyAsync(
            city => city.NormalizedName == normalizedName
                && (!excludedCityId.HasValue || city.Id != excludedCityId.Value),
            cancellationToken);

    public Task<bool> IsReferencedAsync(Guid cityId, CancellationToken cancellationToken) =>
        context.Properties.AnyAsync(property => property.CityId == cityId, cancellationToken);

    public Task AddAsync(City city, CancellationToken cancellationToken) =>
        context.Cities.AddAsync(city, cancellationToken).AsTask();

    public void Remove(City city) => context.Cities.Remove(city);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
