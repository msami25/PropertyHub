namespace PropertyHub.Application.Models.Auth;

public enum CredentialValidationOutcome
{
    Success,
    InvalidCredentials,
    Disabled
}

public sealed record CredentialValidationResult(
    CredentialValidationOutcome Outcome,
    AccountSnapshot? Account)
{
    public static CredentialValidationResult Success(AccountSnapshot account) =>
        new(CredentialValidationOutcome.Success, account);

    public static CredentialValidationResult Invalid() =>
        new(CredentialValidationOutcome.InvalidCredentials, null);

    public static CredentialValidationResult Disabled() =>
        new(CredentialValidationOutcome.Disabled, null);
}
