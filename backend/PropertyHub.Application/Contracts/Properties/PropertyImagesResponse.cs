using PropertyHub.Domain.Enums;

namespace PropertyHub.Application.Contracts.Properties;

public sealed record PropertyImagesResponse(
    Guid PropertyId,
    IReadOnlyList<PropertyImageResponse> Images,
    ModerationStatus ModerationStatus);
