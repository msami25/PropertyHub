namespace PropertyHub.Application.Contracts.Auth;

public sealed record AuthUserResponse(
    Guid Id,
    string FullName,
    string Email,
    string Role);
