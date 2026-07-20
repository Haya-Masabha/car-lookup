using CarLookup.Web.Models;
using CarLookup.Web.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CarLookup.Tests;

public class CachingVehicleCatalogServiceTests
{
    private static CachingVehicleCatalogService CreateService(IVehicleCatalogService inner) =>
        new(inner, new MemoryCache(new MemoryCacheOptions()), Options.Create(new VpicOptions()));

    [Fact]
    public async Task Serves_repeated_make_requests_from_the_cache()
    {
        var inner = new CountingCatalog { Makes = [new Make(474, "Honda")] };
        var service = CreateService(inner);

        await service.GetMakesAsync();
        await service.GetMakesAsync();

        Assert.Equal(1, inner.MakeCalls);
    }

    [Fact]
    public async Task Caches_models_per_make_year_and_vehicle_type()
    {
        var inner = new CountingCatalog { Models = [new VehicleModel(1, "Accord", null)] };
        var service = CreateService(inner);

        await service.GetModelsAsync(474, 2015);
        await service.GetModelsAsync(474, 2015);          // cache hit
        await service.GetModelsAsync(474, 2016);          // different year  -> miss
        await service.GetModelsAsync(474, 2015, "Truck"); // different type  -> miss
        await service.GetModelsAsync(448, 2015);          // different make  -> miss

        Assert.Equal(4, inner.ModelCalls);
    }

    [Fact]
    public async Task Does_not_cache_empty_results_so_a_transient_outage_is_retried()
    {
        var inner = new CountingCatalog { Makes = [] };
        var service = CreateService(inner);

        await service.GetMakesAsync();
        await service.GetMakesAsync();

        Assert.Equal(2, inner.MakeCalls);
    }

    /// <summary>Minimal hand-rolled test double — the interface is small enough not to need a mocking library.</summary>
    private sealed class CountingCatalog : IVehicleCatalogService
    {
        public IReadOnlyList<Make> Makes { get; init; } = [];

        public IReadOnlyList<VehicleType> VehicleTypes { get; init; } = [];

        public IReadOnlyList<VehicleModel> Models { get; init; } = [];

        public int MakeCalls { get; private set; }

        public int VehicleTypeCalls { get; private set; }

        public int ModelCalls { get; private set; }

        public Task<IReadOnlyList<Make>> GetMakesAsync(CancellationToken cancellationToken = default)
        {
            MakeCalls++;
            return Task.FromResult(Makes);
        }

        public Task<IReadOnlyList<VehicleType>> GetVehicleTypesAsync(
            int makeId,
            CancellationToken cancellationToken = default)
        {
            VehicleTypeCalls++;
            return Task.FromResult(VehicleTypes);
        }

        public Task<IReadOnlyList<VehicleModel>> GetModelsAsync(
            int makeId,
            int year,
            string? vehicleType = null,
            CancellationToken cancellationToken = default)
        {
            ModelCalls++;
            return Task.FromResult(Models);
        }
    }
}
