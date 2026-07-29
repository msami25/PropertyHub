using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PropertyHub.Api.Authentication;
using PropertyHub.Application.Contracts.Properties;
using PropertyHub.Application.Interfaces.Services;
using PropertyHub.Application.Models.Properties;
using PropertyHub.Domain.Authorization;

namespace PropertyHub.Api.Controllers;

[ApiController]
[Route("api/properties/{propertyId:guid}/images")]
public sealed class PropertyImagesController(
    IPropertyImageService propertyImageService,
    IAuthorizationService authorizationService) : ControllerBase
{
    private const long MaximumRequestSizeBytes = 26L * 1024 * 1024;

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicyNames.ActiveUser)]
    [EnableRateLimiting("uploads")]
    [RequestSizeLimit(MaximumRequestSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaximumRequestSizeBytes)]
    public async Task<ActionResult<PropertyImagesResponse>> Upload(
        Guid propertyId,
        [FromForm] List<IFormFile> images,
        CancellationToken cancellationToken)
    {
        var uploads = images
            .Select(image => new PropertyImageUpload(
                image.FileName,
                image.ContentType,
                image.Length,
                image.OpenReadStream()))
            .ToArray();

        try
        {
            var result = await propertyImageService.UploadAsync(
                propertyId,
                GetUserId(),
                uploads,
                cancellationToken);
            return MapMutation(result);
        }
        finally
        {
            foreach (var upload in uploads)
            {
                await upload.Content.DisposeAsync();
            }
        }
    }

    [HttpPut("{imageId:guid}/primary")]
    [Authorize(Policy = AuthorizationPolicyNames.ActiveUser)]
    public async Task<ActionResult<PropertyImagesResponse>> SetPrimary(
        Guid propertyId,
        Guid imageId,
        CancellationToken cancellationToken) =>
        MapMutation(await propertyImageService.SetPrimaryAsync(
            propertyId,
            imageId,
            GetUserId(),
            cancellationToken));

    [HttpDelete("{imageId:guid}")]
    [Authorize(Policy = AuthorizationPolicyNames.ActiveUser)]
    public async Task<ActionResult<PropertyImagesResponse>> Delete(
        Guid propertyId,
        Guid imageId,
        CancellationToken cancellationToken) =>
        MapMutation(await propertyImageService.DeleteAsync(
            propertyId,
            imageId,
            GetUserId(),
            cancellationToken));

    [HttpGet("{imageId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(
        Guid propertyId,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        Guid? userId = null;
        var isAdmin = false;
        if (User.Identity?.IsAuthenticated == true)
        {
            var authorization = await authorizationService.AuthorizeAsync(
                User,
                AuthorizationPolicyNames.ActiveUser);
            if (authorization.Succeeded
                && Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedUserId))
            {
                userId = parsedUserId;
                isAdmin = User.IsInRole(RoleNames.Admin);
            }
        }

        var image = await propertyImageService.GetAsync(
            propertyId,
            imageId,
            userId,
            isAdmin,
            cancellationToken);
        if (image is null)
        {
            return NotFound(CreateProblem(404, "Property image not found"));
        }

        Response.Headers.CacheControl = image.IsPublic
            ? "public,max-age=300"
            : "private,no-store";
        return File(image.Content, image.ContentType, enableRangeProcessing: true);
    }

    private ActionResult<PropertyImagesResponse> MapMutation(PropertyImageMutationResult result) =>
        result.Outcome switch
        {
            PropertyImageMutationOutcome.Success when result.PropertyImages is not null =>
                Ok(result.PropertyImages),
            PropertyImageMutationOutcome.NotFound =>
                NotFound(CreateProblem(404, "Property or image not found")),
            PropertyImageMutationOutcome.LimitExceeded =>
                Conflict(CreateProblem(409, result.Error ?? "The image limit has been reached")),
            PropertyImageMutationOutcome.LastImage =>
                Conflict(CreateProblem(409, result.Error ?? "The last image cannot be removed")),
            PropertyImageMutationOutcome.InvalidTransition =>
                Conflict(CreateProblem(409, result.Error ?? "Images cannot be changed in this state")),
            PropertyImageMutationOutcome.Invalid =>
                BadRequest(CreateProblem(400, result.Error ?? "Image validation failed")),
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
