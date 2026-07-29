using PropertyHub.Application.Contracts.Properties;

namespace PropertyHub.Application.Models.Properties;

public enum PropertyImageMutationOutcome
{
    Success,
    NotFound,
    Invalid,
    LimitExceeded,
    LastImage,
    InvalidTransition
}

public sealed record PropertyImageMutationResult(
    PropertyImageMutationOutcome Outcome,
    PropertyImagesResponse? PropertyImages = null,
    string? Error = null);
