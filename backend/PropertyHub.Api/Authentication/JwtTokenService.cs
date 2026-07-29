using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PropertyHub.Application.Contracts.Auth;
using PropertyHub.Application.Interfaces.Services;
using PropertyHub.Application.Models.Auth;
using PropertyHub.Domain.Authorization;

namespace PropertyHub.Api.Authentication;

public sealed class JwtTokenService(
    IOptions<JwtOptions> options,
    TimeProvider timeProvider) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;

    public AuthTokenResponse CreateToken(AccountSnapshot account)
    {
        var issuedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        var expiresAtUtc = issuedAtUtc.AddMinutes(_options.AccessTokenMinutes);
        var role = account.Roles.Contains(RoleNames.Admin, StringComparer.Ordinal)
            ? RoleNames.Admin
            : RoleNames.User;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, account.Email),
            new(ClaimTypes.Name, account.FullName),
            new(ClaimTypes.Role, role),
            new(AuthClaimNames.TokenVersion, account.TokenVersion.ToString())
        };

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: issuedAtUtc,
            expires: expiresAtUtc,
            signingCredentials: signingCredentials);

        return new AuthTokenResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            "Bearer",
            expiresAtUtc,
            new AuthUserResponse(account.Id, account.FullName, account.Email, role));
    }
}
