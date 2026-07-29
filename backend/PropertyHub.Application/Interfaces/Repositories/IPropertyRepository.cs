using PropertyHub.Application.Contracts.Properties;
using PropertyHub.Domain.Entities;
using PropertyHub.Domain.Enums;

namespace PropertyHub.Application.Interfaces.Repositories;

public interface IPropertyRepository
{
    Task<IReadOnlyList<Property>> ListPublicAsync(
        PropertyQueryRequest query,
        CancellationToken cancellationToken);
    Task<Property?> GetPublicByIdAsync(Guid propertyId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Property>> ListOwnedAsync(Guid userId, CancellationToken cancellationToken);
    Task<Property?> GetOwnedAsync(
        Guid propertyId,
        Guid userId,
        bool trackChanges,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<Property>> ListForAdminAsync(
        ModerationStatus? moderationStatus,
        CancellationToken cancellationToken);
    Task<Property?> GetForModerationAsync(Guid propertyId, CancellationToken cancellationToken);
    Task<SellerProfile?> GetSellerProfileAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> ActiveCityExistsAsync(Guid cityId, CancellationToken cancellationToken);
    Task<bool> DuplicateExistsAsync(
        Guid sellerProfileId,
        string normalizedTitle,
        string normalizedAddress,
        PropertyPurpose purpose,
        PropertyType propertyType,
        Guid? excludedPropertyId,
        CancellationToken cancellationToken);
    Task AddSellerProfileAsync(SellerProfile sellerProfile, CancellationToken cancellationToken);
    Task AddPropertyAsync(Property property, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
