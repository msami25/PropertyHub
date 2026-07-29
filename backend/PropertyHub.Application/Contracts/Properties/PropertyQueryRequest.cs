using PropertyHub.Domain.Enums;

namespace PropertyHub.Application.Contracts.Properties;

public sealed record PropertyQueryRequest(
    Guid? CityId,
    PropertyPurpose? Purpose,
    PropertyType? PropertyType);
