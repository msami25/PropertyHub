using PropertyHub.Domain.Entities;

namespace PropertyHub.Application.Interfaces.Repositories;

public interface ICityRepository
{
    Task<IReadOnlyList<City>> ListActiveAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<City>> ListAllAsync(CancellationToken cancellationToken);
    Task<City?> GetByIdAsync(Guid cityId, CancellationToken cancellationToken);
    Task<City?> GetForUpdateAsync(Guid cityId, CancellationToken cancellationToken);
    Task<bool> NameExistsAsync(
        string normalizedName,
        Guid? excludedCityId,
        CancellationToken cancellationToken);
    Task<bool> IsReferencedAsync(Guid cityId, CancellationToken cancellationToken);
    Task AddAsync(City city, CancellationToken cancellationToken);
    void Remove(City city);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
