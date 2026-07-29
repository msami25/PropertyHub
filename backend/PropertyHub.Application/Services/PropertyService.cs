using PropertyHub.Application.Contracts.Properties;
using PropertyHub.Application.Interfaces.Repositories;
using PropertyHub.Application.Interfaces.Services;
using PropertyHub.Application.Models.Properties;
using PropertyHub.Domain.Entities;
using PropertyHub.Domain.Enums;

namespace PropertyHub.Application.Services;

public sealed class PropertyService(
    IPropertyRepository propertyRepository,
    IUserAccountRepository userAccountRepository,
    TimeProvider timeProvider) : IPropertyService
{
    public async Task<PropertyListResponse> ListPublicAsync(
        PropertyQueryRequest query,
        CancellationToken cancellationToken) =>
        new((await propertyRepository.ListPublicAsync(query, cancellationToken)).Select(MapSummary).ToArray());

    public async Task<PropertyDetailResponse?> GetPublicByIdAsync(
        Guid propertyId,
        CancellationToken cancellationToken)
    {
        var property = await propertyRepository.GetPublicByIdAsync(propertyId, cancellationToken);
        return property is null ? null : MapPublicDetail(property);
    }

    public async Task<PropertyManagementListResponse> ListOwnedAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        new((await propertyRepository.ListOwnedAsync(userId, cancellationToken)).Select(MapManagement).ToArray());

    public async Task<PropertyManagementResponse?> GetOwnedAsync(
        Guid propertyId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var property = await propertyRepository.GetOwnedAsync(
            propertyId,
            userId,
            false,
            cancellationToken);
        return property is null ? null : MapManagement(property);
    }

    public async Task<PropertyMutationResult> CreateAsync(
        Guid userId,
        CreatePropertyRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = Validate(request);
        if (validationError is not null)
        {
            return Invalid(validationError);
        }

        if (!await propertyRepository.ActiveCityExistsAsync(request.CityId, cancellationToken))
        {
            return new PropertyMutationResult(
                PropertyMutationOutcome.InactiveCity,
                Error: "Select an active city.");
        }

        var account = await userAccountRepository.GetByIdAsync(userId, cancellationToken);
        if (account is null)
        {
            return new PropertyMutationResult(PropertyMutationOutcome.NotFound);
        }

        var profile = await propertyRepository.GetSellerProfileAsync(userId, cancellationToken);
        if (profile is null)
        {
            profile = new SellerProfile
            {
                UserId = userId,
                DisplayName = account.FullName,
                PhoneNumber = request.ContactNumber.Trim()
            };
            await propertyRepository.AddSellerProfileAsync(profile, cancellationToken);
        }

        var normalizedTitle = Normalize(request.Title);
        var normalizedAddress = Normalize(request.Address);
        if (await propertyRepository.DuplicateExistsAsync(
                profile.Id,
                normalizedTitle,
                normalizedAddress,
                request.Purpose,
                request.PropertyType,
                null,
                cancellationToken))
        {
            return new PropertyMutationResult(
                PropertyMutationOutcome.Duplicate,
                Error: "A matching property listing already exists.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var property = new Property
        {
            SellerProfileId = profile.Id,
            CityId = request.CityId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        Apply(property, request);
        await propertyRepository.AddPropertyAsync(property, cancellationToken);
        await propertyRepository.SaveChangesAsync(cancellationToken);

        var saved = await propertyRepository.GetOwnedAsync(property.Id, userId, false, cancellationToken);
        return new PropertyMutationResult(
            PropertyMutationOutcome.Success,
            saved is null ? null : MapManagement(saved));
    }

    public async Task<PropertyMutationResult> UpdateAsync(
        Guid propertyId,
        Guid userId,
        UpdatePropertyRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = Validate(request);
        if (validationError is not null)
        {
            return Invalid(validationError);
        }

        var property = await propertyRepository.GetOwnedAsync(
            propertyId,
            userId,
            true,
            cancellationToken);
        if (property is null)
        {
            return new PropertyMutationResult(PropertyMutationOutcome.NotFound);
        }
        if (property.AvailabilityStatus is AvailabilityStatus.Sold or AvailabilityStatus.Rented)
        {
            return new PropertyMutationResult(
                PropertyMutationOutcome.InvalidTransition,
                Error: "Sold or rented properties cannot be edited.");
        }
        if (!await propertyRepository.ActiveCityExistsAsync(request.CityId, cancellationToken))
        {
            return new PropertyMutationResult(
                PropertyMutationOutcome.InactiveCity,
                Error: "Select an active city.");
        }

        var normalizedTitle = Normalize(request.Title);
        var normalizedAddress = Normalize(request.Address);
        if (await propertyRepository.DuplicateExistsAsync(
                property.SellerProfileId,
                normalizedTitle,
                normalizedAddress,
                request.Purpose,
                request.PropertyType,
                property.Id,
                cancellationToken))
        {
            return new PropertyMutationResult(
                PropertyMutationOutcome.Duplicate,
                Error: "A matching property listing already exists.");
        }

        Apply(property, request);
        property.ModerationStatus = ModerationStatus.Pending;
        property.RejectionReason = null;
        property.ModeratedAtUtc = null;
        property.ModeratedByUserId = null;
        property.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        await propertyRepository.SaveChangesAsync(cancellationToken);
        return new PropertyMutationResult(PropertyMutationOutcome.Success, MapManagement(property));
    }

    public async Task<PropertyMutationResult> UpdateAvailabilityAsync(
        Guid propertyId,
        Guid userId,
        UpdateAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var property = await propertyRepository.GetOwnedAsync(
            propertyId,
            userId,
            true,
            cancellationToken);
        if (property is null)
        {
            return new PropertyMutationResult(PropertyMutationOutcome.NotFound);
        }
        if (property.AvailabilityStatus != AvailabilityStatus.Available
            || request.Status == AvailabilityStatus.Available
            || (property.Purpose == PropertyPurpose.Sale && request.Status != AvailabilityStatus.Sold)
            || (property.Purpose == PropertyPurpose.Rent && request.Status != AvailabilityStatus.Rented))
        {
            return new PropertyMutationResult(
                PropertyMutationOutcome.InvalidTransition,
                Error: "The requested availability transition is not allowed.");
        }

        property.AvailabilityStatus = request.Status;
        property.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        await propertyRepository.SaveChangesAsync(cancellationToken);
        return new PropertyMutationResult(PropertyMutationOutcome.Success, MapManagement(property));
    }

    public async Task<PropertyMutationOutcome> DeleteAsync(
        Guid propertyId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var property = await propertyRepository.GetOwnedAsync(
            propertyId,
            userId,
            true,
            cancellationToken);
        if (property is null)
        {
            return PropertyMutationOutcome.NotFound;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        property.IsDeleted = true;
        property.DeletedAtUtc = now;
        property.UpdatedAtUtc = now;
        await propertyRepository.SaveChangesAsync(cancellationToken);
        return PropertyMutationOutcome.Success;
    }

    public async Task<PropertyManagementListResponse> ListForAdminAsync(
        ModerationStatus? moderationStatus,
        CancellationToken cancellationToken) =>
        new((await propertyRepository.ListForAdminAsync(moderationStatus, cancellationToken))
            .Select(MapManagement)
            .ToArray());

    public async Task<PropertyMutationResult> ModerateAsync(
        Guid propertyId,
        Guid adminUserId,
        ModeratePropertyRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Status is not (ModerationStatus.Approved or ModerationStatus.Rejected))
        {
            return Invalid("Moderation must approve or reject the property.");
        }
        var reason = request.Reason?.Trim();
        if (request.Status == ModerationStatus.Rejected && string.IsNullOrWhiteSpace(reason))
        {
            return Invalid("A rejection reason is required.");
        }

        var property = await propertyRepository.GetForModerationAsync(propertyId, cancellationToken);
        if (property is null)
        {
            return new PropertyMutationResult(PropertyMutationOutcome.NotFound);
        }
        if (property.ModerationStatus != ModerationStatus.Pending)
        {
            return new PropertyMutationResult(
                PropertyMutationOutcome.InvalidTransition,
                Error: "Only pending properties can be moderated.");
        }
        if (request.Status == ModerationStatus.Approved && property.Images.Count == 0)
        {
            return new PropertyMutationResult(
                PropertyMutationOutcome.InvalidTransition,
                Error: "A property requires at least one image before approval.");
        }

        property.ModerationStatus = request.Status;
        property.RejectionReason = request.Status == ModerationStatus.Rejected ? reason : null;
        property.ModeratedByUserId = adminUserId;
        property.ModeratedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        property.UpdatedAtUtc = property.ModeratedAtUtc.Value;
        await propertyRepository.SaveChangesAsync(cancellationToken);
        return new PropertyMutationResult(PropertyMutationOutcome.Success, MapManagement(property));
    }

    private static PropertyMutationResult Invalid(string error) =>
        new(PropertyMutationOutcome.Invalid, Error: error);

    private static string? Validate(CreatePropertyRequest request) =>
        ValidateValues(
            request.Title,
            request.Description,
            request.Address,
            request.ContactNumber,
            request.Purpose,
            request.PropertyType,
            request.AreaUnit,
            request.Bedrooms,
            request.Bathrooms);

    private static string? Validate(UpdatePropertyRequest request) =>
        ValidateValues(
            request.Title,
            request.Description,
            request.Address,
            request.ContactNumber,
            request.Purpose,
            request.PropertyType,
            request.AreaUnit,
            request.Bedrooms,
            request.Bathrooms);

    private static string? ValidateValues(
        string title,
        string description,
        string address,
        string contactNumber,
        PropertyPurpose purpose,
        PropertyType propertyType,
        AreaUnit areaUnit,
        int? bedrooms,
        int? bathrooms)
    {
        if (title.Trim().Length is < 5 or > 100) return "Title must contain 5 to 100 characters.";
        if (description.Trim().Length is < 20 or > 2000)
            return "Description must contain 20 to 2000 characters.";
        if (address.Trim().Length is < 5 or > 250) return "Address must contain 5 to 250 characters.";
        if (contactNumber.Trim().Length is < 3 or > 20)
            return "Contact number must contain 3 to 20 characters.";
        if (!Enum.IsDefined(purpose) || !Enum.IsDefined(propertyType) || !Enum.IsDefined(areaUnit))
            return "Select supported property values.";

        return propertyType switch
        {
            PropertyType.House or PropertyType.Apartment when bedrooms is null or <= 0
                || bathrooms is null or <= 0 =>
                "Houses and apartments require positive bedroom and bathroom values.",
            PropertyType.Plot when bedrooms is not null || bathrooms is not null =>
                "Plots cannot have bedroom or bathroom values.",
            PropertyType.Shop or PropertyType.Office when bedrooms < 0 || bathrooms < 0 =>
                "Bedroom and bathroom values cannot be negative.",
            _ => null
        };
    }

    private static void Apply(Property property, CreatePropertyRequest request)
    {
        property.Title = request.Title.Trim();
        property.NormalizedTitle = Normalize(request.Title);
        property.Description = request.Description.Trim();
        property.Purpose = request.Purpose;
        property.PropertyType = request.PropertyType;
        property.CityId = request.CityId;
        property.Address = request.Address.Trim();
        property.NormalizedAddress = Normalize(request.Address);
        property.Price = request.Price;
        property.Area = request.Area;
        property.AreaUnit = request.AreaUnit;
        property.AreaSquareFeet = ConvertArea(request.Area, request.AreaUnit);
        property.Bedrooms = request.Bedrooms;
        property.Bathrooms = request.Bathrooms;
        property.ContactNumber = request.ContactNumber.Trim();
    }

    private static void Apply(Property property, UpdatePropertyRequest request)
    {
        property.Title = request.Title.Trim();
        property.NormalizedTitle = Normalize(request.Title);
        property.Description = request.Description.Trim();
        property.Purpose = request.Purpose;
        property.PropertyType = request.PropertyType;
        property.CityId = request.CityId;
        property.Address = request.Address.Trim();
        property.NormalizedAddress = Normalize(request.Address);
        property.Price = request.Price;
        property.Area = request.Area;
        property.AreaUnit = request.AreaUnit;
        property.AreaSquareFeet = ConvertArea(request.Area, request.AreaUnit);
        property.Bedrooms = request.Bedrooms;
        property.Bathrooms = request.Bathrooms;
        property.ContactNumber = request.ContactNumber.Trim();
    }

    private static decimal ConvertArea(decimal area, AreaUnit areaUnit) =>
        decimal.Round(area * (areaUnit switch
        {
            AreaUnit.SquareFeet => 1m,
            AreaUnit.Marla => 272.25m,
            AreaUnit.Kanal => 5445m,
            _ => 0m
        }), 2);

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();

    private static PropertyCityResponse MapCity(City city) => new(city.Id, city.Name);

    private static PropertySummaryResponse MapSummary(Property property) =>
        new(
            property.Id,
            property.Title,
            MapCity(property.City),
            property.Purpose,
            property.PropertyType,
            property.Price,
            property.Area,
            property.AreaUnit,
            property.Bedrooms,
            property.Bathrooms,
            property.Images
                .Where(image => image.IsPrimary)
                .Select(image => PropertyImageService.MapImage(image).Url)
                .SingleOrDefault());

    private static PropertyDetailResponse MapPublicDetail(Property property) =>
        new(
            property.Id,
            property.Title,
            property.Description,
            MapCity(property.City),
            property.Purpose,
            property.PropertyType,
            property.Address,
            property.Price,
            property.Area,
            property.AreaUnit,
            property.Bedrooms,
            property.Bathrooms,
            property.SellerProfile.DisplayName,
            MapImages(property));

    private static PropertyManagementResponse MapManagement(Property property) =>
        new(
            property.Id,
            property.Title,
            property.Description,
            MapCity(property.City),
            property.Purpose,
            property.PropertyType,
            property.Address,
            property.Price,
            property.Area,
            property.AreaUnit,
            property.Bedrooms,
            property.Bathrooms,
            property.ContactNumber,
            property.ModerationStatus,
            property.AvailabilityStatus,
            property.RejectionReason,
            property.CreatedAtUtc,
            property.UpdatedAtUtc,
            MapImages(property));

    private static IReadOnlyList<PropertyImageResponse> MapImages(Property property) =>
        property.Images
            .OrderBy(image => image.SortOrder)
            .Select(PropertyImageService.MapImage)
            .ToArray();
}
