using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PropertyHub.Api.Authentication;
using PropertyHub.Application.Contracts.Auth;
using PropertyHub.Application.Interfaces.Services;
using PropertyHub.Application.Models.Auth;

namespace PropertyHub.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<RegistrationResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request, cancellationToken);
        if (!result.Succeeded || result.Response is null)
        {
            var details = new ValidationProblemDetails(
                result.Errors.ToDictionary(pair => pair.Key, pair => pair.Value))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Instance = HttpContext.Request.Path
            };
            details.Extensions["traceId"] = HttpContext.TraceIdentifier;
            return BadRequest(details);
        }

        return CreatedAtAction(nameof(Me), result.Response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthTokenResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        return result.Outcome switch
        {
            LoginOutcome.Success when result.Response is not null => Ok(result.Response),
            LoginOutcome.Disabled => StatusCode(
                StatusCodes.Status403Forbidden,
                CreateProblem(StatusCodes.Status403Forbidden, "Account disabled")),
            _ => Unauthorized(CreateProblem(StatusCodes.Status401Unauthorized, "Invalid credentials"))
        };
    }

    [HttpGet("me")]
    [Authorize(Policy = AuthorizationPolicyNames.ActiveUser)]
    public async Task<ActionResult<AuthUserResponse>> Me(CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpGet("/api/admin/session")]
    [Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
    public async Task<ActionResult<AuthUserResponse>> AdminSession(CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    private async Task<AuthUserResponse?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId)
            ? await authService.GetCurrentUserAsync(userId, cancellationToken)
            : null;
    }

    private ProblemDetails CreateProblem(int status, string title) =>
        new()
        {
            Status = status,
            Title = title,
            Instance = HttpContext.Request.Path,
            Extensions = { ["traceId"] = HttpContext.TraceIdentifier }
        };
}
