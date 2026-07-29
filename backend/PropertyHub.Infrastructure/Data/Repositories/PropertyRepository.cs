using Microsoft.EntityFrameworkCore;
using PropertyHub.Application.Contracts.Properties;
using PropertyHub.Application.Interfaces.Repositories;
using PropertyHub.Domain.Entities;
using PropertyHub.Domain.Enums;

namespace PropertyHub.Infrastructure.Data.Repositories;

public sealed class PropertyRepository(ApplicationDbContext context) : IPropertyRepository
{
    public async Task<IReadOnlyList<Property>> ListPublicAsync(
        PropertyQueryRequest query,
        CancellationToken cancellationToken)
    {
        var properties = PublicQuery();
        if (query.CityId.HasValue)
        {
            properties = properties.Where(property => property.CityId == query.CityId.Value);
        }
        if (query.Purpose.HasValue)
        {
            properties = properties.Where(property => property.Purpose == query.Purpose.Value);
        }
        if (query.PropertyType.HasValue)
        {
            properties = properties.Where(property => property.PropertyType == query.PropertyType.Value);
        }

        return await properties.OrderByDescending(property => property.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<Property?> GetPublicByIdAsync(Guid propertyId, CancellationToken cancellationToken) =>
        PublicQuery().SingleOrDefaultAsync(property => property.Id == propertyId, cancellationToken);

    public async Task<IReadOnlyList<Property>> ListOwnedAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        await BaseQuery(true)
            .Where(property => property.SellerProfile.UserId == userId && !property.IsDeleted)
            .OrderByDescending(property => property.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<Property?> GetOwnedAsync(
        Guid propertyId,
        Guid userId,
        bool trackChanges,
        CancellationToken cancellationToken) =>
        BaseQuery(!trackChanges).SingleOrDefaultAsync(
            property => property.Id == propertyId
                && property.SellerProfile.UserId == userId
                && !property.IsDeleted,
            cancellationToken);

    public async Task<IReadOnlyList<Property>> ListForAdminAsync(
        ModerationStatus? moderationStatus,
        CancellationToken cancellationToken)
    {
        var properties = BaseQuery(true).Where(property => !property.IsDeleted);
        if (moderationStatus.HasValue)
        {
            properties = properties.Where(
                property => property.ModerationStatus == moderationStatus.Value);
        }

        return await properties.OrderByDescending(property => property.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<Property?> GetForModerationAsync(
        Guid propertyId,
        CancellationToken cancellationToken) =>
        BaseQuery(false).SingleOrDefaultAsync(
            property => property.Id == propertyId && !property.IsDeleted,
            cancellationToken);

    public Task<SellerProfile?> GetSellerProfileAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        context.SellerProfiles.SingleOrDefaultAsync(
            profile => profile.UserId == userId,
            cancellationToken);

    public Task<bool> ActiveCityExistsAsync(Guid cityId, CancellationToken cancellationToken) =>
        context.Cities.AnyAsync(city => city.Id == cityId && city.IsActive, cancellationToken);

    public Task<bool> DuplicateExistsAsync(
        Guid sellerProfileId,
        string normalizedTitle,
        string normalizedAddress,
        PropertyPurpose purpose,
        PropertyType propertyType,
        Guid? excludedPropertyId,
        CancellationToken cancellationToken) =>
        context.Properties.AnyAsync(
            property => property.SellerProfileId == sellerProfileId
                && property.NormalizedTitle == normalizedTitle
                && property.NormalizedAddress == normalizedAddress
                && property.Purpose == purpose
                && property.PropertyType == propertyType
                && !property.IsDeleted
                && (!excludedPropertyId.HasValue || property.Id != excludedPropertyId.Value),
            cancellationToken);

    public Task AddSellerProfileAsync(
        SellerProfile sellerProfile,
        CancellationToken cancellationToken) =>
        context.SellerProfiles.AddAsync(sellerProfile, cancellationToken).AsTask();

    public Task AddPropertyAsync(Property property, CancellationToken cancellationToken) =>
        context.Properties.AddAsync(property, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);

    private IQueryable<Property> PublicQuery() =>
        BaseQuery(true).Where(property =>
            property.ModerationStatus == ModerationStatus.Approved
            && property.AvailabilityStatus == AvailabilityStatus.Available
            && !property.IsDeleted
            && property.Images.Any()
            && context.Users.Any(user =>
                user.Id == property.SellerProfile.UserId
                && user.Status == AccountStatus.Active));

    private IQueryable<Property> BaseQuery(bool noTracking)
    {
        var query = context.Properties
            .Include(property => property.City)
            .Include(property => property.SellerProfile)
            .Include(property => property.Images)
            .AsQueryable();
        return noTracking ? query.AsNoTracking() : query;
    }
}
