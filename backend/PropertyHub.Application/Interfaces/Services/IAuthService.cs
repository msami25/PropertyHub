using PropertyHub.Application.Contracts.Auth;
using PropertyHub.Application.Models.Auth;

namespace PropertyHub.Application.Interfaces.Services;

public interface IAuthService
{
    Task<RegistrationResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthUserResponse?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
}
