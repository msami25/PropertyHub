using PropertyHub.Application.Models.Auth;

namespace PropertyHub.Application.Interfaces.Repositories;

public interface IUserAccountRepository
{
    Task<AccountCreationResult> CreateUserAsync(
        string fullName,
        string email,
        string password,
        CancellationToken cancellationToken);

    Task<CredentialValidationResult> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken);

    Task<AccountSnapshot?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<bool> IsActiveAsync(Guid userId, int tokenVersion, CancellationToken cancellationToken);
}
