using CarLookup.Web.Models;

namespace CarLookup.Web.Services;

/// <summary>
/// Read access to the vehicle catalog. Implemented by <see cref="VpicVehicleCatalogService"/>
/// (live HTTP) and wrapped by <see cref="CachingVehicleCatalogService"/> (in-memory cache).
/// </summary>
public interface IVehicleCatalogService
{
    /// <summary>Every make known to vPIC (~12,000 rows).</summary>
    Task<IReadOnlyList<Make>> GetMakesAsync(CancellationToken cancellationToken = default);

    /// <summary>Vehicle types produced by a given make.</summary>
    Task<IReadOnlyList<VehicleType>> GetVehicleTypesAsync(int makeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Models for a make in a model year, optionally narrowed to a single vehicle type.
    /// </summary>
    Task<IReadOnlyList<VehicleModel>> GetModelsAsync(
        int makeId,
        int year,
        string? vehicleType = null,
        CancellationToken cancellationToken = default);
}
