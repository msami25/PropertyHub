using Microsoft.EntityFrameworkCore;
using PropertyHub.Application.Interfaces.Repositories;
using PropertyHub.Domain.Entities;
using PropertyHub.Domain.Enums;

namespace PropertyHub.Infrastructure.Data.Repositories;

public sealed class PropertyImageRepository(ApplicationDbContext context) : IPropertyImageRepository
{
    public Task<PropertyImage?> GetForAccessAsync(
        Guid propertyId,
        Guid imageId,
        CancellationToken cancellationToken) =>
        context.PropertyImages
            .AsNoTracking()
            .Include(image => image.Property)
            .ThenInclude(property => property.SellerProfile)
            .SingleOrDefaultAsync(
                image => image.Id == imageId
                    && image.PropertyId == propertyId
                    && !image.Property.IsDeleted,
                cancellationToken);

    public Task<bool> IsPubliclyVisibleAsync(
        Guid propertyId,
        CancellationToken cancellationToken) =>
        context.Properties.AnyAsync(
            property => property.Id == propertyId
                && property.ModerationStatus == ModerationStatus.Approved
                && property.AvailabilityStatus == AvailabilityStatus.Available
                && !property.IsDeleted
                && property.Images.Any()
                && context.Users.Any(user =>
                    user.Id == property.SellerProfile.UserId
                    && user.Status == AccountStatus.Active),
            cancellationToken);

    public Task AddRangeAsync(
        IReadOnlyCollection<PropertyImage> images,
        CancellationToken cancellationToken) =>
        context.PropertyImages.AddRangeAsync(images, cancellationToken);

    public void Remove(PropertyImage image) => context.PropertyImages.Remove(image);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
