using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using PropertyHub.Application.Interfaces.Repositories;

namespace PropertyHub.Api.Authentication;

public sealed class ActiveUserAuthorizationHandler(
    IUserAccountRepository userAccountRepository) : AuthorizationHandler<ActiveUserRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveUserRequirement requirement)
    {
        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var tokenVersionValue = context.User.FindFirstValue(AuthClaimNames.TokenVersion);

        if (!Guid.TryParse(userIdValue, out var userId) ||
            !int.TryParse(tokenVersionValue, out var tokenVersion))
        {
            return;
        }

        if (await userAccountRepository.IsActiveAsync(userId, tokenVersion, CancellationToken.None))
        {
            context.Succeed(requirement);
        }
    }
}
