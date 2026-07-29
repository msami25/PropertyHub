using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyHub.Api.Authentication;
using PropertyHub.Application.Contracts.Admin;
using PropertyHub.Application.Interfaces.Services;
using PropertyHub.Application.Models.Admin;

namespace PropertyHub.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
public sealed class AdminController(IAdminService adminService) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<AdminDashboardResponse>> Dashboard(
        CancellationToken cancellationToken) =>
        Ok(await adminService.GetDashboardAsync(cancellationToken));

    [HttpGet("users")]
    public async Task<ActionResult<AdminUserListResponse>> ListUsers(
        [FromQuery] AdminUserQueryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminService.ListUsersAsync(request, cancellationToken);
        return result.Succeeded && result.Response is not null
            ? Ok(result.Response)
            : BadRequest(CreateProblem(400, result.Error ?? "The user query is invalid."));
    }

    [HttpPatch("users/{userId:guid}/role")]
    public async Task<ActionResult<AdminUserResponse>> ChangeRole(
        Guid userId,
        ChangeUserRoleRequest request,
        [FromHeader(Name = "If-Match")] string? expectedVersion,
        CancellationToken cancellationToken)
    {
        var result = await adminService.ChangeRoleAsync(
            CurrentUserId(),
            userId,
            expectedVersion,
            request,
            cancellationToken);
        return MapMutation(result);
    }

    [HttpPatch("users/{userId:guid}/status")]
    public async Task<ActionResult<AdminUserResponse>> ChangeStatus(
        Guid userId,
        ChangeUserStatusRequest request,
        [FromHeader(Name = "If-Match")] string? expectedVersion,
        CancellationToken cancellationToken)
    {
        var result = await adminService.ChangeStatusAsync(
            CurrentUserId(),
            userId,
            expectedVersion,
            request,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return MapMutation(result);
    }

    private ActionResult<AdminUserResponse> MapMutation(AdminUserMutationResult result) =>
        result.Outcome switch
        {
            AdminUserMutationOutcome.Success when result.User is not null => Ok(result.User),
            AdminUserMutationOutcome.NotFound => NotFound(CreateProblem(404, "User not found.")),
            AdminUserMutationOutcome.Invalid =>
                BadRequest(CreateProblem(400, result.Error ?? "The request is invalid.")),
            AdminUserMutationOutcome.Conflict =>
                Conflict(CreateProblem(409, result.Error ?? "The account cannot be changed.")),
            AdminUserMutationOutcome.VersionMismatch =>
                StatusCode(412, CreateProblem(
                    412,
                    result.Error ?? "The account changed after it was loaded.")),
            _ => StatusCode(500, CreateProblem(500, "An unexpected error occurred."))
        };

    private Guid CurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private ProblemDetails CreateProblem(int status, string title) =>
        new()
        {
            Status = status,
            Title = title,
            Instance = HttpContext.Request.Path,
            Extensions = { ["traceId"] = HttpContext.TraceIdentifier }
        };
}
