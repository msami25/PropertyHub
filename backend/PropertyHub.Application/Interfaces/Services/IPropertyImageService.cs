using PropertyHub.Application.Models.Properties;

namespace PropertyHub.Application.Interfaces.Services;

public interface IPropertyImageService
{
    Task<PropertyImageMutationResult> UploadAsync(
        Guid propertyId,
        Guid userId,
        IReadOnlyList<PropertyImageUpload> uploads,
        CancellationToken cancellationToken);
    Task<PropertyImageMutationResult> SetPrimaryAsync(
        Guid propertyId,
        Guid imageId,
        Guid userId,
        CancellationToken cancellationToken);
    Task<PropertyImageMutationResult> DeleteAsync(
        Guid propertyId,
        Guid imageId,
        Guid userId,
        CancellationToken cancellationToken);
    Task<PropertyImageFileResult?> GetAsync(
        Guid propertyId,
        Guid imageId,
        Guid? userId,
        bool isAdmin,
        CancellationToken cancellationToken);
}
