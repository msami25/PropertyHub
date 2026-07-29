using PropertyHub.Domain.Enums;

namespace PropertyHub.Application.Contracts.Properties;

public sealed record PropertySummaryResponse(
    Guid Id,
    string Title,
    PropertyCityResponse City,
    PropertyPurpose Purpose,
    PropertyType PropertyType,
    decimal Price,
    decimal Area,
    AreaUnit AreaUnit,
    int? Bedrooms,
    int? Bathrooms);
