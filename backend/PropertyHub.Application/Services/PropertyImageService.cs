using PropertyHub.Application.Contracts.Properties;
using PropertyHub.Application.Interfaces.Repositories;
using PropertyHub.Application.Interfaces.Services;
using PropertyHub.Application.Models.Properties;
using PropertyHub.Domain.Entities;
using PropertyHub.Domain.Enums;

namespace PropertyHub.Application.Services;

public sealed class PropertyImageService(
    IPropertyRepository propertyRepository,
    IPropertyImageRepository imageRepository,
    IImageStorage imageStorage,
    PropertyImageValidator validator,
    TimeProvider timeProvider) : IPropertyImageService
{
    public async Task<PropertyImageMutationResult> UploadAsync(
        Guid propertyId,
        Guid userId,
        IReadOnlyList<PropertyImageUpload> uploads,
        CancellationToken cancellationToken)
    {
        var property = await propertyRepository.GetOwnedAsync(
            propertyId,
            userId,
            true,
            cancellationToken);
        if (property is null)
        {
            return NotFound();
        }
        if (property.AvailabilityStatus is AvailabilityStatus.Sold or AvailabilityStatus.Rented)
        {
            return InvalidTransition("Sold or rented properties cannot receive images.");
        }
        if (uploads.Count is < 1 or > PropertyImageValidator.MaximumImageCount)
        {
            return Invalid("Upload between 1 and 5 images.");
        }
        if (property.Images.Count + uploads.Count > PropertyImageValidator.MaximumImageCount)
        {
            return new PropertyImageMutationResult(
                PropertyImageMutationOutcome.LimitExceeded,
                Error: "A property can contain no more than 5 images.");
        }

        var validatedUploads = new List<ValidatedUpload>(uploads.Count);
        foreach (var upload in uploads)
        {
            var bytes = await ReadUploadAsync(upload, cancellationToken);
            if (bytes is null)
            {
                return Invalid("Each image must contain data and be no larger than 5 MB.");
            }

            var validation = validator.Validate(
                upload.FileName,
                upload.ContentType,
                upload.Length,
                bytes);
            if (!validation.IsValid)
            {
                return Invalid(validation.Error!);
            }

            validatedUploads.Add(new ValidatedUpload(
                validation.SafeOriginalFileName!,
                validation.CanonicalContentType!,
                validation.CanonicalExtension!,
                bytes));
        }

        var availableSortOrders = Enumerable.Range(1, PropertyImageValidator.MaximumImageCount)
            .Select(value => (byte)value)
            .Except(property.Images.Select(image => image.SortOrder))
            .Take(validatedUploads.Count)
            .ToArray();
        var newImages = new List<PropertyImage>(validatedUploads.Count);
        var savedPaths = new List<string>(validatedUploads.Count);
        var hasPrimary = property.Images.Any(image => image.IsPrimary);

        try
        {
            for (var index = 0; index < validatedUploads.Count; index++)
            {
                var upload = validatedUploads[index];
                var storedFileName = $"{Guid.NewGuid():N}{upload.Extension}";
                var relativePath = $"{property.Id:N}/{storedFileName}";
                await imageStorage.SaveAsync(relativePath, upload.Content, cancellationToken);
                savedPaths.Add(relativePath);

                var image = new PropertyImage
                {
                    PropertyId = property.Id,
                    OriginalFileName = upload.OriginalFileName,
                    StoredFileName = storedFileName,
                    RelativePath = relativePath,
                    ContentType = upload.ContentType,
                    FileSizeBytes = upload.Content.LongLength,
                    SortOrder = availableSortOrders[index],
                    IsPrimary = !hasPrimary && index == 0,
                    UploadedAtUtc = timeProvider.GetUtcNow().UtcDateTime
                };
                newImages.Add(image);
                property.Images.Add(image);
            }

            ResetModeration(property);
            await imageRepository.AddRangeAsync(newImages, cancellationToken);
            await imageRepository.SaveChangesAsync(cancellationToken);
            return Success(property);
        }
        catch
        {
            foreach (var relativePath in savedPaths)
            {
                await imageStorage.DeleteAsync(relativePath, CancellationToken.None);
            }
            throw;
        }
    }

    public async Task<PropertyImageMutationResult> SetPrimaryAsync(
        Guid propertyId,
        Guid imageId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var property = await propertyRepository.GetOwnedAsync(
            propertyId,
            userId,
            true,
            cancellationToken);
        if (property is null)
        {
            return NotFound();
        }
        if (property.AvailabilityStatus is AvailabilityStatus.Sold or AvailabilityStatus.Rented)
        {
            return InvalidTransition("Sold or rented properties cannot change their primary image.");
        }

        var selectedImage = property.Images.SingleOrDefault(image => image.Id == imageId);
        if (selectedImage is null)
        {
            return NotFound();
        }
        if (selectedImage.IsPrimary)
        {
            return Success(property);
        }

        foreach (var image in property.Images)
        {
            image.IsPrimary = image.Id == imageId;
        }
        ResetModeration(property);
        await imageRepository.SaveChangesAsync(cancellationToken);
        return Success(property);
    }

    public async Task<PropertyImageMutationResult> DeleteAsync(
        Guid propertyId,
        Guid imageId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var property = await propertyRepository.GetOwnedAsync(
            propertyId,
            userId,
            true,
            cancellationToken);
        if (property is null)
        {
            return NotFound();
        }
        if (property.AvailabilityStatus is AvailabilityStatus.Sold or AvailabilityStatus.Rented)
        {
            return InvalidTransition("Sold or rented properties cannot remove images.");
        }

        var image = property.Images.SingleOrDefault(item => item.Id == imageId);
        if (image is null)
        {
            return NotFound();
        }
        if (property.Images.Count == 1)
        {
            return new PropertyImageMutationResult(
                PropertyImageMutationOutcome.LastImage,
                Error: "A property must retain at least one image.");
        }

        imageRepository.Remove(image);
        property.Images.Remove(image);
        if (image.IsPrimary)
        {
            property.Images.OrderBy(item => item.SortOrder).First().IsPrimary = true;
        }
        ResetModeration(property);
        await imageRepository.SaveChangesAsync(cancellationToken);
        await imageStorage.DeleteAsync(image.RelativePath, cancellationToken);
        return Success(property);
    }

    public async Task<PropertyImageFileResult?> GetAsync(
        Guid propertyId,
        Guid imageId,
        Guid? userId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        var image = await imageRepository.GetForAccessAsync(
            propertyId,
            imageId,
            cancellationToken);
        if (image is null)
        {
            return null;
        }

        var isPublic = await imageRepository.IsPubliclyVisibleAsync(
            propertyId,
            cancellationToken);
        var isOwner = userId.HasValue && image.Property.SellerProfile.UserId == userId.Value;
        if (!isPublic && !isOwner && !isAdmin)
        {
            return null;
        }

        var content = await imageStorage.OpenReadAsync(image.RelativePath, cancellationToken);
        return new PropertyImageFileResult(
            content,
            image.ContentType,
            image.FileSizeBytes,
            isPublic);
    }

    private async Task<byte[]?> ReadUploadAsync(
        PropertyImageUpload upload,
        CancellationToken cancellationToken)
    {
        if (upload.Length is <= 0 or > PropertyImageValidator.MaximumFileSizeBytes)
        {
            return null;
        }

        using var buffer = new MemoryStream((int)upload.Length);
        await upload.Content.CopyToAsync(buffer, cancellationToken);
        return buffer.Length is > 0 and <= PropertyImageValidator.MaximumFileSizeBytes
            ? buffer.ToArray()
            : null;
    }

    private void ResetModeration(Property property)
    {
        property.ModerationStatus = ModerationStatus.Pending;
        property.RejectionReason = null;
        property.ModeratedByUserId = null;
        property.ModeratedAtUtc = null;
        property.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
    }

    private static PropertyImageMutationResult Success(Property property) =>
        new(PropertyImageMutationOutcome.Success, Map(property));

    private static PropertyImageMutationResult NotFound() =>
        new(PropertyImageMutationOutcome.NotFound);

    private static PropertyImageMutationResult Invalid(string error) =>
        new(PropertyImageMutationOutcome.Invalid, Error: error);

    private static PropertyImageMutationResult InvalidTransition(string error) =>
        new(PropertyImageMutationOutcome.InvalidTransition, Error: error);

    private static PropertyImagesResponse Map(Property property) =>
        new(
            property.Id,
            property.Images
                .OrderBy(image => image.SortOrder)
                .Select(MapImage)
                .ToArray(),
            property.ModerationStatus);

    public static PropertyImageResponse MapImage(PropertyImage image) =>
        new(
            image.Id,
            $"/api/properties/{image.PropertyId}/images/{image.Id}",
            image.SortOrder,
            image.IsPrimary,
            image.ContentType,
            image.FileSizeBytes);

    private sealed record ValidatedUpload(
        string OriginalFileName,
        string ContentType,
        string Extension,
        byte[] Content);
}
