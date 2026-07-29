using Microsoft.AspNetCore.Identity;
using PropertyHub.Application.Interfaces.Repositories;
using PropertyHub.Application.Models.Auth;
using PropertyHub.Domain.Authorization;
using PropertyHub.Domain.Enums;

namespace PropertyHub.Infrastructure.Identity;

public sealed class IdentityUserAccountRepository(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : IUserAccountRepository
{
    public async Task<AccountCreationResult> CreateUserAsync(
        string fullName,
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            UserName = email,
            Email = email,
            Status = AccountStatus.Active,
            EmailConfirmed = true
        };

        var creation = await userManager.CreateAsync(user, password);
        if (!creation.Succeeded)
        {
            return AccountCreationResult.Failure(ToErrorDictionary(creation.Errors));
        }

        var roleResult = await userManager.AddToRoleAsync(user, RoleNames.User);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return AccountCreationResult.Failure(ToErrorDictionary(roleResult.Errors));
        }

        return AccountCreationResult.Success(await ToSnapshotAsync(user));
    }

    public async Task<CredentialValidationResult> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return CredentialValidationResult.Invalid();
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (!signInResult.Succeeded)
        {
            return CredentialValidationResult.Invalid();
        }

        if (user.Status != AccountStatus.Active)
        {
            return CredentialValidationResult.Disabled();
        }

        return CredentialValidationResult.Success(await ToSnapshotAsync(user));
    }

    public async Task<AccountSnapshot?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : await ToSnapshotAsync(user);
    }

    public async Task<bool> IsActiveAsync(
        Guid userId,
        int tokenVersion,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is { Status: AccountStatus.Active } && user.TokenVersion == tokenVersion;
    }

    private async Task<AccountSnapshot> ToSnapshotAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new AccountSnapshot(
            user.Id,
            user.FullName,
            user.Email!,
            user.Status,
            user.TokenVersion,
            roles.ToArray());
    }

    private static IReadOnlyDictionary<string, string[]> ToErrorDictionary(
        IEnumerable<IdentityError> errors) =>
        errors.GroupBy(error => GetErrorKey(error.Code))
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Description).ToArray());

    private static string GetErrorKey(string code) =>
        code.Contains("Password", StringComparison.OrdinalIgnoreCase) ? "password" : "email";
}
