using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CarLookup.Tests;

/// <summary>
/// Builds the minimum controller context a unit-tested controller needs: <c>ValidationProblem</c>
/// resolves <see cref="ProblemDetailsFactory"/> from the request services, which are absent from a
/// bare <see cref="DefaultHttpContext"/>.
/// </summary>
internal static class ControllerContextFactory
{
    public static ControllerContext Create()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers();

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() }
        };
    }
}
