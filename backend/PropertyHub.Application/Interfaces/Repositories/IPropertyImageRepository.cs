using PropertyHub.Domain.Entities;

namespace PropertyHub.Application.Interfaces.Repositories;

public interface IPropertyImageRepository
{
    Task<PropertyImage?> GetForAccessAsync(
        Guid propertyId,
        Guid imageId,
        CancellationToken cancellationToken);
    Task<bool> IsPubliclyVisibleAsync(Guid propertyId, CancellationToken cancellationToken);
    Task AddRangeAsync(
        IReadOnlyCollection<PropertyImage> images,
        CancellationToken cancellationToken);
    void Remove(PropertyImage image);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
