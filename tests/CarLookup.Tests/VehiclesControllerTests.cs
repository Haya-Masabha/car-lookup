using CarLookup.Web.Controllers;
using CarLookup.Web.Models;
using CarLookup.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CarLookup.Tests;

public class VehiclesControllerTests
{
    private static VehiclesController CreateController(IVehicleCatalogService? catalog = null) =>
        new(catalog ?? new EmptyCatalog())
        {
            // ValidationProblem needs a ProblemDetailsFactory resolved from the request services.
            ControllerContext = ControllerContextFactory.Create()
        };

    [Fact]
    public async Task Rejects_a_model_year_before_vpic_has_data()
    {
        var result = await CreateController().GetModels(474, year: 1980, vehicleType: null);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Rejects_a_model_year_too_far_in_the_future()
    {
        var result = await CreateController().GetModels(474, year: DateTime.UtcNow.Year + 5, vehicleType: null);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Rejects_a_non_positive_make_id()
    {
        var result = await CreateController().GetVehicleTypes(0);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Accepts_a_valid_request()
    {
        var result = await CreateController().GetModels(474, year: 2015, vehicleType: null);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public void Year_list_runs_from_the_earliest_supported_year_to_next_year_newest_first()
    {
        var years = Assert.IsType<OkObjectResult>(CreateController().GetYears().Result).Value;

        var list = Assert.IsAssignableFrom<IReadOnlyList<int>>(years);

        Assert.Equal(DateTime.UtcNow.Year + 1, list[0]);
        Assert.Equal(VehiclesController.EarliestModelYear, list[^1]);
        Assert.Equal(list.OrderByDescending(y => y), list);
    }

    private sealed class EmptyCatalog : IVehicleCatalogService
    {
        public Task<IReadOnlyList<Make>> GetMakesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Make>>([]);

        public Task<IReadOnlyList<VehicleType>> GetVehicleTypesAsync(
            int makeId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<VehicleType>>([]);

        public Task<IReadOnlyList<VehicleModel>> GetModelsAsync(
            int makeId,
            int year,
            string? vehicleType = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<VehicleModel>>([]);
    }
}
