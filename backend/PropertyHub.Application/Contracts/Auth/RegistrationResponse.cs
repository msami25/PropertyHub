namespace PropertyHub.Application.Contracts.Auth;

public sealed record RegistrationResponse(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    string Status);
