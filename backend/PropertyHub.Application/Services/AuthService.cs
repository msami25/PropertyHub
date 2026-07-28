using PropertyHub.Application.Contracts.Auth;
using PropertyHub.Application.Interfaces.Repositories;
using PropertyHub.Application.Interfaces.Services;
using PropertyHub.Application.Models.Auth;
using PropertyHub.Domain.Authorization;

namespace PropertyHub.Application.Services;

public sealed class AuthService(
    IUserAccountRepository userAccountRepository,
    IJwtTokenService jwtTokenService) : IAuthService
{
    public async Task<RegistrationResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userAccountRepository.CreateUserAsync(
            request.FullName.Trim(),
            request.Email.Trim(),
            request.Password,
            cancellationToken);

        if (!result.Succeeded || result.Account is null)
        {
            return new RegistrationResult(null, result.Errors);
        }

        var response = new RegistrationResponse(
            result.Account.Id,
            result.Account.FullName,
            result.Account.Email,
            RoleNames.User,
            result.Account.Status.ToString());

        return new RegistrationResult(response, result.Errors);
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var validation = await userAccountRepository.ValidateCredentialsAsync(
            request.Email.Trim(),
            request.Password,
            cancellationToken);

        return validation.Outcome switch
        {
            CredentialValidationOutcome.Success when validation.Account is not null =>
                LoginResult.Success(jwtTokenService.CreateToken(validation.Account)),
            CredentialValidationOutcome.Disabled => LoginResult.Disabled(),
            _ => LoginResult.Invalid()
        };
    }

    public async Task<AuthUserResponse?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var account = await userAccountRepository.GetByIdAsync(userId, cancellationToken);
        if (account is null)
        {
            return null;
        }

        var role = account.Roles.Contains(RoleNames.Admin, StringComparer.Ordinal)
            ? RoleNames.Admin
            : RoleNames.User;

        return new AuthUserResponse(account.Id, account.FullName, account.Email, role);
    }
}
