using CarLookup.Api.Models;
using CarLookup.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CarLookup.Api.Controllers;

/// <summary>
/// JSON endpoints consumed by the browser. The page never calls vPIC directly: routing it through
/// the server lets the app cache responses, keep the upstream contract in one place, and avoid
/// depending on vPIC's CORS policy.
/// </summary>
[ApiController]
[Route("api/vehicles")]
[Produces("application/json")]
public sealed class VehiclesController : ControllerBase
{
    /// <summary>vPIC has no model data before the 1995 model year.</summary>
    public const int EarliestModelYear = 1995;

    private readonly IVehicleCatalogService _catalog;

    public VehiclesController(IVehicleCatalogService catalog)
    {
        _catalog = catalog;
    }

    /// <summary>Makes matching a search term, ranked and capped for the type-ahead.</summary>
    [HttpGet("makes")]
    [ProducesResponseType<IReadOnlyList<Make>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Make>>> GetMakes(
        [FromQuery] string? query,
        [FromQuery] int limit = MakeSearch.DefaultLimit,
        CancellationToken cancellationToken = default)
    {
        var makes = await _catalog.GetMakesAsync(cancellationToken);

        return Ok(MakeSearch.Filter(makes, query, limit));
    }

    /// <summary>Vehicle types produced by a make.</summary>
    [HttpGet("makes/{makeId:int}/vehicle-types")]
    [ProducesResponseType<IReadOnlyList<VehicleType>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VehicleType>>> GetVehicleTypes(
        int makeId,
        CancellationToken cancellationToken = default)
    {
        if (makeId <= 0)
        {
            return ValidationProblem("Make id must be a positive number.");
        }

        return Ok(await _catalog.GetVehicleTypesAsync(makeId, cancellationToken));
    }

    /// <summary>Models for a make and model year, optionally narrowed to one vehicle type.</summary>
    [HttpGet("makes/{makeId:int}/models")]
    [ProducesResponseType<IReadOnlyList<VehicleModel>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VehicleModel>>> GetModels(
        int makeId,
        [FromQuery] int year,
        [FromQuery] string? vehicleType,
        CancellationToken cancellationToken = default)
    {
        if (makeId <= 0)
        {
            return ValidationProblem("Make id must be a positive number.");
        }

        var latestYear = DateTime.UtcNow.Year + 1;
        if (year < EarliestModelYear || year > latestYear)
        {
            return ValidationProblem($"Model year must be between {EarliestModelYear} and {latestYear}.");
        }

        return Ok(await _catalog.GetModelsAsync(makeId, year, vehicleType, cancellationToken));
    }

    /// <summary>Selectable model years, newest first.</summary>
    [HttpGet("years")]
    [ProducesResponseType<IReadOnlyList<int>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<int>> GetYears()
    {
        var latestYear = DateTime.UtcNow.Year + 1;

        return Ok(Enumerable.Range(EarliestModelYear, latestYear - EarliestModelYear + 1)
            .Reverse()
            .ToList());
    }
}
