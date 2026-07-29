using PropertyHub.Domain.Enums;

namespace PropertyHub.Application.Contracts.Properties;

public sealed record PropertyManagementResponse(
    Guid Id,
    string Title,
    string Description,
    PropertyCityResponse City,
    PropertyPurpose Purpose,
    PropertyType PropertyType,
    string Address,
    decimal Price,
    decimal Area,
    AreaUnit AreaUnit,
    int? Bedrooms,
    int? Bathrooms,
    string ContactNumber,
    ModerationStatus ModerationStatus,
    AvailabilityStatus AvailabilityStatus,
    string? RejectionReason,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<PropertyImageResponse> Images);
