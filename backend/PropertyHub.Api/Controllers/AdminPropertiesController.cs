using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyHub.Api.Authentication;
using PropertyHub.Application.Contracts.Properties;
using PropertyHub.Application.Interfaces.Services;
using PropertyHub.Application.Models.Properties;
using PropertyHub.Domain.Enums;

namespace PropertyHub.Api.Controllers;

[ApiController]
[Route("api/admin/properties")]
[Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
public sealed class AdminPropertiesController(IPropertyService propertyService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PropertyManagementListResponse>> List(
        [FromQuery] ModerationStatus? moderationStatus,
        CancellationToken cancellationToken) =>
        Ok(await propertyService.ListForAdminAsync(moderationStatus, cancellationToken));

    [HttpPost("{propertyId:guid}/moderation")]
    public async Task<ActionResult<PropertyManagementResponse>> Moderate(
        Guid propertyId,
        ModeratePropertyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await propertyService.ModerateAsync(
            propertyId,
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            request,
            cancellationToken);
        return result.Outcome switch
        {
            PropertyMutationOutcome.Success when result.Property is not null => Ok(result.Property),
            PropertyMutationOutcome.NotFound =>
                NotFound(CreateProblem(404, "Property not found")),
            PropertyMutationOutcome.InvalidTransition =>
                Conflict(CreateProblem(409, result.Error ?? "The property cannot be moderated")),
            PropertyMutationOutcome.Invalid =>
                BadRequest(CreateProblem(400, result.Error ?? "Moderation validation failed")),
            _ => StatusCode(500, CreateProblem(500, "An unexpected error occurred"))
        };
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
