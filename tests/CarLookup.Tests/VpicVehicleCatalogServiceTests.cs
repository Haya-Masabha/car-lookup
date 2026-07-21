using System.Net;
using CarLookup.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarLookup.Tests;

public class VpicVehicleCatalogServiceTests
{
    private static VpicVehicleCatalogService CreateService(StubHttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://vpic.example/api/vehicles/") },
            NullLogger<VpicVehicleCatalogService>.Instance);

    [Fact]
    public async Task Maps_makes_and_normalises_their_casing()
    {
        var handler = StubHttpMessageHandler.ReturningJson("""
            {
              "Count": 3,
              "Results": [
                { "Make_ID": 474, "Make_Name": "HONDA" },
                { "Make_ID": 452, "Make_Name": "BMW" },
                { "Make_ID": 999, "Make_Name": "ASTON MARTIN" }
              ]
            }
            """);

        var makes = await CreateService(handler).GetMakesAsync();

        // Sorted alphabetically; short all-caps names such as BMW are left alone.
        Assert.Equal(["Aston Martin", "BMW", "Honda"], makes.Select(m => m.MakeName));
        Assert.Equal(474, makes.Single(m => m.MakeName == "Honda").MakeId);
    }

    [Fact]
    public async Task Maps_vehicle_types()
    {
        var handler = StubHttpMessageHandler.ReturningJson("""
            {
              "Count": 2,
              "Results": [
                { "VehicleTypeId": 3, "VehicleTypeName": "Truck" },
                { "VehicleTypeId": 2, "VehicleTypeName": "Passenger Car" }
              ]
            }
            """);

        var types = await CreateService(handler).GetVehicleTypesAsync(474);

        Assert.Equal(["Passenger Car", "Truck"], types.Select(t => t.VehicleTypeName));
        Assert.Contains("GetVehicleTypesForMakeId/474", handler.Requests.Single().ToString());
    }

    [Fact]
    public async Task Calls_the_unfiltered_route_when_no_vehicle_type_is_given()
    {
        var handler = StubHttpMessageHandler.ReturningJson("""{ "Count": 0, "Results": [] }""");

        await CreateService(handler).GetModelsAsync(474, 2015);

        Assert.EndsWith(
            "GetModelsForMakeIdYear/makeId/474/modelyear/2015?format=json",
            handler.Requests.Single().ToString());
    }

    [Fact]
    public async Task Calls_the_vehicle_type_route_and_escapes_the_type()
    {
        var handler = StubHttpMessageHandler.ReturningJson("""
            {
              "Count": 1,
              "Results": [
                { "Make_ID": 474, "Model_ID": 1866, "Model_Name": "Ridgeline", "VehicleTypeName": "Truck" }
              ]
            }
            """);

        var models = await CreateService(handler).GetModelsAsync(474, 2015, "Passenger Car");

        // AbsoluteUri, not ToString(): the latter unescapes the value being asserted on.
        Assert.Contains("vehicletype/Passenger%20Car", handler.Requests.Single().AbsoluteUri);

        var model = Assert.Single(models);
        Assert.Equal(1866, model.ModelId);
        Assert.Equal("Ridgeline", model.ModelName);
        Assert.Equal("Truck", model.VehicleTypeName);
    }

    [Fact]
    public async Task Returns_an_empty_list_when_vpic_reports_no_results()
    {
        var handler = StubHttpMessageHandler.ReturningJson(
            """{ "Count": 0, "Message": "Response returned successfully", "Results": [] }""");

        Assert.Empty(await CreateService(handler).GetModelsAsync(474, 2015));
    }

    [Fact]
    public async Task Throws_a_domain_exception_when_vpic_fails()
    {
        var handler = StubHttpMessageHandler.ReturningStatus(HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<VpicUnavailableException>(() => CreateService(handler).GetMakesAsync());
    }

    [Fact]
    public async Task Throws_a_domain_exception_when_vpic_returns_something_that_is_not_json()
    {
        var handler = StubHttpMessageHandler.ReturningJson("<html>down for maintenance</html>");

        await Assert.ThrowsAsync<VpicUnavailableException>(() => CreateService(handler).GetMakesAsync());
    }
}
