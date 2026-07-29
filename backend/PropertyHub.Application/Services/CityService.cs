using PropertyHub.Application.Contracts.Cities;
using PropertyHub.Application.Interfaces.Repositories;
using PropertyHub.Application.Interfaces.Services;
using PropertyHub.Application.Models.Cities;
using PropertyHub.Domain.Entities;

namespace PropertyHub.Application.Services;

public sealed class CityService(ICityRepository cityRepository) : ICityService
{
    public async Task<CityListResponse> ListActiveAsync(CancellationToken cancellationToken) =>
        new((await cityRepository.ListActiveAsync(cancellationToken)).Select(Map).ToArray());

    public async Task<CityListResponse> ListAllAsync(CancellationToken cancellationToken) =>
        new((await cityRepository.ListAllAsync(cancellationToken)).Select(Map).ToArray());

    public async Task<CityResponse?> GetByIdAsync(Guid cityId, CancellationToken cancellationToken)
    {
        var city = await cityRepository.GetByIdAsync(cityId, cancellationToken);
        return city is null ? null : Map(city);
    }

    public async Task<CityMutationResult> CreateAsync(
        CreateCityRequest request,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (name.Length is < 2 or > 100)
        {
            return new CityMutationResult(CityMutationOutcome.InvalidName);
        }

        var normalizedName = NormalizeName(name);
        if (await cityRepository.NameExistsAsync(normalizedName, null, cancellationToken))
        {
            return new CityMutationResult(CityMutationOutcome.DuplicateName);
        }

        var city = new City
        {
            Name = name,
            NormalizedName = normalizedName,
            IsActive = request.IsActive,
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };
        await cityRepository.AddAsync(city, cancellationToken);
        await cityRepository.SaveChangesAsync(cancellationToken);
        return new CityMutationResult(CityMutationOutcome.Success, Map(city));
    }

    public async Task<CityMutationResult> UpdateAsync(
        Guid cityId,
        UpdateCityRequest request,
        CancellationToken cancellationToken)
    {
        var city = await cityRepository.GetForUpdateAsync(cityId, cancellationToken);
        if (city is null)
        {
            return new CityMutationResult(CityMutationOutcome.NotFound);
        }

        var name = request.Name.Trim();
        if (name.Length is < 2 or > 100)
        {
            return new CityMutationResult(CityMutationOutcome.InvalidName);
        }

        var normalizedName = NormalizeName(name);
        if (await cityRepository.NameExistsAsync(normalizedName, cityId, cancellationToken))
        {
            return new CityMutationResult(CityMutationOutcome.DuplicateName);
        }

        city.Name = name;
        city.NormalizedName = normalizedName;
        city.IsActive = request.IsActive;
        city.Latitude = request.Latitude;
        city.Longitude = request.Longitude;
        await cityRepository.SaveChangesAsync(cancellationToken);
        return new CityMutationResult(CityMutationOutcome.Success, Map(city));
    }

    public async Task<CityMutationOutcome> DeleteAsync(
        Guid cityId,
        CancellationToken cancellationToken)
    {
        var city = await cityRepository.GetForUpdateAsync(cityId, cancellationToken);
        if (city is null)
        {
            return CityMutationOutcome.NotFound;
        }

        if (await cityRepository.IsReferencedAsync(cityId, cancellationToken))
        {
            return CityMutationOutcome.InUse;
        }

        cityRepository.Remove(city);
        await cityRepository.SaveChangesAsync(cancellationToken);
        return CityMutationOutcome.Success;
    }

    private static string NormalizeName(string name) => name.ToUpperInvariant();

    private static CityResponse Map(City city) =>
        new(city.Id, city.Name, city.IsActive, city.Latitude, city.Longitude);
}
