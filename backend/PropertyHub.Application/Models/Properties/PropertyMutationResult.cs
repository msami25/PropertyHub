using PropertyHub.Application.Contracts.Properties;

namespace PropertyHub.Application.Models.Properties;

public sealed record PropertyMutationResult(
    PropertyMutationOutcome Outcome,
    PropertyManagementResponse? Property = null,
    string? Error = null);

public enum PropertyMutationOutcome
{
    Success,
    NotFound,
    Invalid,
    Duplicate,
    InactiveCity,
    InvalidTransition
}
