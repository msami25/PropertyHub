using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyHub.Api.Authentication;
using PropertyHub.Application.Contracts.Properties;
using PropertyHub.Application.Interfaces.Services;
using PropertyHub.Application.Models.Properties;

namespace PropertyHub.Api.Controllers;

[ApiController]
[Route("api/properties")]
public sealed class PropertiesController(IPropertyService propertyService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PropertyListResponse>> List(
        [FromQuery] PropertyQueryRequest query,
        CancellationToken cancellationToken) =>
        Ok(await propertyService.ListPublicAsync(query, cancellationToken));

    [HttpGet("{propertyId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<PropertyDetailResponse>> GetById(
        Guid propertyId,
        CancellationToken cancellationToken)
    {
        var property = await propertyService.GetPublicByIdAsync(propertyId, cancellationToken);
        return property is null ? NotFound(CreateProblem(404, "Property not found")) : Ok(property);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicyNames.ActiveUser)]
    public async Task<ActionResult<PropertyManagementResponse>> Create(
        CreatePropertyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await propertyService.CreateAsync(GetUserId(), request, cancellationToken);
        return result.Outcome switch
        {
            PropertyMutationOutcome.Success when result.Property is not null =>
                CreatedAtAction(
                    nameof(MyPropertiesController.GetById),
                    "MyProperties",
                    new { propertyId = result.Property.Id },
                    result.Property),
            _ => MapMutation(result)
        };
    }

    [HttpPut("{propertyId:guid}")]
    [Authorize(Policy = AuthorizationPolicyNames.ActiveUser)]
    public async Task<ActionResult<PropertyManagementResponse>> Update(
        Guid propertyId,
        UpdatePropertyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await propertyService.UpdateAsync(
            propertyId,
            GetUserId(),
            request,
            cancellationToken);
        return result.Outcome == PropertyMutationOutcome.Success && result.Property is not null
            ? Ok(result.Property)
            : MapMutation(result);
    }

    [HttpPatch("{propertyId:guid}/availability")]
    [Authorize(Policy = AuthorizationPolicyNames.ActiveUser)]
    public async Task<ActionResult<PropertyManagementResponse>> UpdateAvailability(
        Guid propertyId,
        UpdateAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await propertyService.UpdateAvailabilityAsync(
            propertyId,
            GetUserId(),
            request,
            cancellationToken);
        return result.Outcome == PropertyMutationOutcome.Success && result.Property is not null
            ? Ok(result.Property)
            : MapMutation(result);
    }

    [HttpDelete("{propertyId:guid}")]
    [Authorize(Policy = AuthorizationPolicyNames.ActiveUser)]
    public async Task<IActionResult> Delete(Guid propertyId, CancellationToken cancellationToken)
    {
        var outcome = await propertyService.DeleteAsync(propertyId, GetUserId(), cancellationToken);
        return outcome == PropertyMutationOutcome.Success
            ? NoContent()
            : NotFound(CreateProblem(404, "Property not found"));
    }

    private ActionResult<PropertyManagementResponse> MapMutation(PropertyMutationResult result) =>
        result.Outcome switch
        {
            PropertyMutationOutcome.NotFound =>
                NotFound(CreateProblem(404, "Property not found")),
            PropertyMutationOutcome.Duplicate =>
                Conflict(CreateProblem(409, result.Error ?? "A duplicate property exists")),
            PropertyMutationOutcome.InvalidTransition =>
                Conflict(CreateProblem(409, result.Error ?? "The property state cannot be changed")),
            PropertyMutationOutcome.Invalid or PropertyMutationOutcome.InactiveCity =>
                BadRequest(CreateProblem(400, result.Error ?? "Property validation failed")),
            _ => StatusCode(500, CreateProblem(500, "An unexpected error occurred"))
        };

    private Guid GetUserId() =>
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
