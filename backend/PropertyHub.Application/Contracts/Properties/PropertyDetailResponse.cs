using PropertyHub.Domain.Enums;

namespace PropertyHub.Application.Contracts.Properties;

public sealed record PropertyDetailResponse(
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
    string SellerDisplayName,
    IReadOnlyList<PropertyImageResponse> Images);
