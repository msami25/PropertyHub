using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyHub.Application.Contracts.Cities;
using PropertyHub.Application.Interfaces.Services;

namespace PropertyHub.Api.Controllers;

[ApiController]
[Route("api/cities")]
public sealed class CitiesController(ICityService cityService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<CityListResponse>> List(CancellationToken cancellationToken) =>
        Ok(await cityService.ListActiveAsync(cancellationToken));
}
