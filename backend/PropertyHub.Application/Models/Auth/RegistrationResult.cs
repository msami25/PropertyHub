using PropertyHub.Application.Contracts.Auth;

namespace PropertyHub.Application.Models.Auth;

public sealed record RegistrationResult(
    RegistrationResponse? Response,
    IReadOnlyDictionary<string, string[]> Errors)
{
    public bool Succeeded => Response is not null;
}
