using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyHub.Api.Authentication;
using PropertyHub.Application.Contracts.Cities;
using PropertyHub.Application.Interfaces.Services;
using PropertyHub.Application.Models.Cities;

namespace PropertyHub.Api.Controllers;

[ApiController]
[Route("api/admin/cities")]
[Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
public sealed class AdminCitiesController(ICityService cityService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CityListResponse>> List(CancellationToken cancellationToken) =>
        Ok(await cityService.ListAllAsync(cancellationToken));

    [HttpGet("{cityId:guid}")]
    public async Task<ActionResult<CityResponse>> GetById(
        Guid cityId,
        CancellationToken cancellationToken)
    {
        var city = await cityService.GetByIdAsync(cityId, cancellationToken);
        return city is null ? NotFound(CreateProblem(404, "City not found")) : Ok(city);
    }

    [HttpPost]
    public async Task<ActionResult<CityResponse>> Create(
        CreateCityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await cityService.CreateAsync(request, cancellationToken);
        return result.Outcome switch
        {
            CityMutationOutcome.Success when result.City is not null =>
                CreatedAtAction(nameof(GetById), new { cityId = result.City.Id }, result.City),
            CityMutationOutcome.InvalidName =>
                BadRequest(CreateProblem(400, "City name must contain 2 to 100 characters")),
            CityMutationOutcome.DuplicateName =>
                Conflict(CreateProblem(409, "A city with this name already exists")),
            _ => StatusCode(500)
        };
    }

    [HttpPut("{cityId:guid}")]
    public async Task<ActionResult<CityResponse>> Update(
        Guid cityId,
        UpdateCityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await cityService.UpdateAsync(cityId, request, cancellationToken);
        return result.Outcome switch
        {
            CityMutationOutcome.Success when result.City is not null => Ok(result.City),
            CityMutationOutcome.NotFound => NotFound(CreateProblem(404, "City not found")),
            CityMutationOutcome.InvalidName =>
                BadRequest(CreateProblem(400, "City name must contain 2 to 100 characters")),
            CityMutationOutcome.DuplicateName =>
                Conflict(CreateProblem(409, "A city with this name already exists")),
            _ => StatusCode(500)
        };
    }

    [HttpDelete("{cityId:guid}")]
    public async Task<IActionResult> Delete(Guid cityId, CancellationToken cancellationToken)
    {
        var outcome = await cityService.DeleteAsync(cityId, cancellationToken);
        return outcome switch
        {
            CityMutationOutcome.Success => NoContent(),
            CityMutationOutcome.NotFound => NotFound(CreateProblem(404, "City not found")),
            CityMutationOutcome.InUse =>
                Conflict(CreateProblem(409, "The city cannot be deleted while properties use it")),
            _ => StatusCode(500)
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
