using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyHub.Api.Authentication;
using PropertyHub.Application.Contracts.Properties;
using PropertyHub.Application.Interfaces.Services;

namespace PropertyHub.Api.Controllers;

[ApiController]
[Route("api/users/me/properties")]
[Authorize(Policy = AuthorizationPolicyNames.ActiveUser)]
public sealed class MyPropertiesController(IPropertyService propertyService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PropertyManagementListResponse>> List(
        CancellationToken cancellationToken) =>
        Ok(await propertyService.ListOwnedAsync(GetUserId(), cancellationToken));

    [HttpGet("{propertyId:guid}", Name = "MyProperties")]
    public async Task<ActionResult<PropertyManagementResponse>> GetById(
        Guid propertyId,
        CancellationToken cancellationToken)
    {
        var property = await propertyService.GetOwnedAsync(
            propertyId,
            GetUserId(),
            cancellationToken);
        return property is null ? NotFound(CreateProblem(404, "Property not found")) : Ok(property);
    }

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
