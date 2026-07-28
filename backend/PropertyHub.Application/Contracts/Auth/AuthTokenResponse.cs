namespace PropertyHub.Application.Contracts.Auth;

public sealed record AuthTokenResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    AuthUserResponse User);
