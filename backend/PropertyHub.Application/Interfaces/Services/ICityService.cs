using PropertyHub.Application.Contracts.Cities;
using PropertyHub.Application.Models.Cities;

namespace PropertyHub.Application.Interfaces.Services;

public interface ICityService
{
    Task<CityListResponse> ListActiveAsync(CancellationToken cancellationToken);
    Task<CityListResponse> ListAllAsync(CancellationToken cancellationToken);
    Task<CityResponse?> GetByIdAsync(Guid cityId, CancellationToken cancellationToken);
    Task<CityMutationResult> CreateAsync(
        CreateCityRequest request,
        CancellationToken cancellationToken);
    Task<CityMutationResult> UpdateAsync(
        Guid cityId,
        UpdateCityRequest request,
        CancellationToken cancellationToken);
    Task<CityMutationOutcome> DeleteAsync(Guid cityId, CancellationToken cancellationToken);
}
