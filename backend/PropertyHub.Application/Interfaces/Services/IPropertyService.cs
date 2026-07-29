using PropertyHub.Application.Contracts.Properties;
using PropertyHub.Application.Models.Properties;
using PropertyHub.Domain.Enums;

namespace PropertyHub.Application.Interfaces.Services;

public interface IPropertyService
{
    Task<PropertyListResponse> ListPublicAsync(
        PropertyQueryRequest query,
        CancellationToken cancellationToken);
    Task<PropertyDetailResponse?> GetPublicByIdAsync(
        Guid propertyId,
        CancellationToken cancellationToken);
    Task<PropertyManagementListResponse> ListOwnedAsync(
        Guid userId,
        CancellationToken cancellationToken);
    Task<PropertyManagementResponse?> GetOwnedAsync(
        Guid propertyId,
        Guid userId,
        CancellationToken cancellationToken);
    Task<PropertyMutationResult> CreateAsync(
        Guid userId,
        CreatePropertyRequest request,
        CancellationToken cancellationToken);
    Task<PropertyMutationResult> UpdateAsync(
        Guid propertyId,
        Guid userId,
        UpdatePropertyRequest request,
        CancellationToken cancellationToken);
    Task<PropertyMutationResult> UpdateAvailabilityAsync(
        Guid propertyId,
        Guid userId,
        UpdateAvailabilityRequest request,
        CancellationToken cancellationToken);
    Task<PropertyMutationOutcome> DeleteAsync(
        Guid propertyId,
        Guid userId,
        CancellationToken cancellationToken);
    Task<PropertyManagementListResponse> ListForAdminAsync(
        ModerationStatus? moderationStatus,
        CancellationToken cancellationToken);
    Task<PropertyMutationResult> ModerateAsync(
        Guid propertyId,
        Guid adminUserId,
        ModeratePropertyRequest request,
        CancellationToken cancellationToken);
}
