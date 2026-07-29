using System.ComponentModel.DataAnnotations;
using PropertyHub.Domain.Enums;

namespace PropertyHub.Application.Contracts.Properties;

public sealed record UpdatePropertyRequest(
    [Required, StringLength(100, MinimumLength = 5)] string Title,
    [Required, StringLength(2000, MinimumLength = 20)] string Description,
    PropertyPurpose Purpose,
    PropertyType PropertyType,
    Guid CityId,
    [Required, StringLength(250, MinimumLength = 5)] string Address,
    [Range(typeof(decimal), "0.01", "9999999999999999")] decimal Price,
    [Range(typeof(decimal), "0.01", "9999999999")] decimal Area,
    AreaUnit AreaUnit,
    int? Bedrooms,
    int? Bathrooms,
    [Required, StringLength(20, MinimumLength = 3)] string ContactNumber);
