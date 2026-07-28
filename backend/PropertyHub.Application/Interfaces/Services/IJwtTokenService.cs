using PropertyHub.Application.Contracts.Auth;
using PropertyHub.Application.Models.Auth;

namespace PropertyHub.Application.Interfaces.Services;

public interface IJwtTokenService
{
    AuthTokenResponse CreateToken(AccountSnapshot account);
}
