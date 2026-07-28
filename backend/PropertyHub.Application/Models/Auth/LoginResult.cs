using PropertyHub.Application.Contracts.Auth;

namespace PropertyHub.Application.Models.Auth;

public enum LoginOutcome
{
    Success,
    InvalidCredentials,
    Disabled
}

public sealed record LoginResult(LoginOutcome Outcome, AuthTokenResponse? Response)
{
    public static LoginResult Success(AuthTokenResponse response) => new(LoginOutcome.Success, response);
    public static LoginResult Invalid() => new(LoginOutcome.InvalidCredentials, null);
    public static LoginResult Disabled() => new(LoginOutcome.Disabled, null);
}
