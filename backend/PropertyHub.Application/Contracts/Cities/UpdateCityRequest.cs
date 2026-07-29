using System.ComponentModel.DataAnnotations;

namespace PropertyHub.Application.Contracts.Cities;

public sealed record UpdateCityRequest(
    [Required, StringLength(100, MinimumLength = 2)] string Name,
    [Range(typeof(decimal), "-90", "90")] decimal Latitude,
    [Range(typeof(decimal), "-180", "180")] decimal Longitude,
    bool IsActive);
