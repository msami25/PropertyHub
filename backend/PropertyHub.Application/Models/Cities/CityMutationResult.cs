using PropertyHub.Application.Contracts.Cities;

namespace PropertyHub.Application.Models.Cities;

public sealed record CityMutationResult(CityMutationOutcome Outcome, CityResponse? City = null);

public enum CityMutationOutcome
{
    Success,
    NotFound,
    InvalidName,
    DuplicateName,
    InUse
}
