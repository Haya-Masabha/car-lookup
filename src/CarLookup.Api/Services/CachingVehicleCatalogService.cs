using CarLookup.Api.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CarLookup.Api.Services;

/// <summary>
/// Caches catalog lookups in memory. vPIC data is effectively static, so this removes almost all
/// upstream traffic, keeps the UI responsive, and protects the app if vPIC gets slow.
/// </summary>
/// <remarks>
/// Decorates <see cref="IVehicleCatalogService"/> rather than caching inside the HTTP client, so
/// the two concerns can be tested independently.
/// </remarks>
public sealed class CachingVehicleCatalogService : IVehicleCatalogService
{
    private readonly IVehicleCatalogService _inner;
    private readonly IMemoryCache _cache;
    private readonly VpicOptions _options;

    public CachingVehicleCatalogService(
        IVehicleCatalogService inner,
        IMemoryCache cache,
        IOptions<VpicOptions> options)
    {
        _inner = inner;
        _cache = cache;
        _options = options.Value;
    }

    public Task<IReadOnlyList<Make>> GetMakesAsync(CancellationToken cancellationToken = default) =>
        GetOrCreateAsync(
            "vpic:makes",
            _options.MakesCacheDuration,
            () => _inner.GetMakesAsync(cancellationToken));

    public Task<IReadOnlyList<VehicleType>> GetVehicleTypesAsync(
        int makeId,
        CancellationToken cancellationToken = default) =>
        GetOrCreateAsync(
            $"vpic:types:{makeId}",
            _options.LookupCacheDuration,
            () => _inner.GetVehicleTypesAsync(makeId, cancellationToken));

    public Task<IReadOnlyList<VehicleModel>> GetModelsAsync(
        int makeId,
        int year,
        string? vehicleType = null,
        CancellationToken cancellationToken = default) =>
        GetOrCreateAsync(
            $"vpic:models:{makeId}:{year}:{vehicleType?.ToLowerInvariant() ?? "*"}",
            _options.LookupCacheDuration,
            () => _inner.GetModelsAsync(makeId, year, vehicleType, cancellationToken));

    private async Task<IReadOnlyList<T>> GetOrCreateAsync<T>(
        string key,
        TimeSpan duration,
        Func<Task<IReadOnlyList<T>>> factory)
    {
        if (_cache.TryGetValue(key, out IReadOnlyList<T>? cached) && cached is not null)
        {
            return cached;
        }

        var value = await factory();

        // Empty results are not cached: they are usually a sign of a transient upstream hiccup,
        // and re-asking later is cheap.
        if (value.Count > 0)
        {
            _cache.Set(key, value, duration);
        }

        return value;
    }
}
