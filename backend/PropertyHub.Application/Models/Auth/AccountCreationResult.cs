namespace PropertyHub.Application.Models.Auth;

public sealed record AccountCreationResult(
    bool Succeeded,
    AccountSnapshot? Account,
    IReadOnlyDictionary<string, string[]> Errors)
{
    public static AccountCreationResult Success(AccountSnapshot account) =>
        new(true, account, new Dictionary<string, string[]>());

    public static AccountCreationResult Failure(IReadOnlyDictionary<string, string[]> errors) =>
        new(false, null, errors);
}
