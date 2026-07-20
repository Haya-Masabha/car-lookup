using CarLookup.Web.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CarLookup.Web.Infrastructure;

/// <summary>
/// Turns an upstream vPIC outage into a clean 503 problem response for API callers, so the
/// browser can show a friendly message instead of a stack trace. Anything else falls through
/// to the standard MVC error page.
/// </summary>
public sealed class VpicExceptionHandler : IExceptionHandler
{
    private readonly ILogger<VpicExceptionHandler> _logger;

    public VpicExceptionHandler(ILogger<VpicExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not VpicUnavailableException ||
            !httpContext.Request.Path.StartsWithSegments("/api"))
        {
            return false;
        }

        _logger.LogWarning(exception, "Serving 503 because vPIC is unavailable.");

        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Vehicle data service unavailable",
                Detail = "The NHTSA vPIC service could not be reached. Please try again in a moment."
            },
            cancellationToken);

        return true;
    }
}
