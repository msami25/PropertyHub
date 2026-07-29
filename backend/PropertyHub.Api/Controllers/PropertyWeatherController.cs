using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyHub.Application.Contracts.Weather;
using PropertyHub.Application.Interfaces.Services;
using PropertyHub.Application.Models.Weather;

namespace PropertyHub.Api.Controllers;

[ApiController]
[Route("api/properties/{propertyId:guid}/weather")]
public sealed class PropertyWeatherController(
    IPropertyWeatherService propertyWeatherService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PropertyWeatherResponse>> Get(
        Guid propertyId,
        CancellationToken cancellationToken)
    {
        var result = await propertyWeatherService.GetAsync(propertyId, cancellationToken);
        return result.Outcome == PropertyWeatherOutcome.Success && result.Weather is not null
            ? Ok(result.Weather)
            : NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Property not found",
                Instance = HttpContext.Request.Path,
                Extensions = { ["traceId"] = HttpContext.TraceIdentifier }
            });
    }
}
