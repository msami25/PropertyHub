using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PropertyHub.Domain.Exceptions;

namespace PropertyHub.Api.Middleware;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var isExpected = exception is DomainException;
        var statusCode = isExpected
            ? StatusCodes.Status400BadRequest
            : StatusCodes.Status500InternalServerError;

        if (isExpected)
        {
            logger.LogWarning("A domain request failed with trace identifier {TraceId}", httpContext.TraceIdentifier);
        }
        else
        {
            logger.LogError(exception, "An unhandled request failed with trace identifier {TraceId}", httpContext.TraceIdentifier);
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = isExpected ? "The request could not be completed." : "An unexpected error occurred.",
            Detail = isExpected ? exception.Message : "The server could not complete the request.",
            Instance = httpContext.Request.Path
        };
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
