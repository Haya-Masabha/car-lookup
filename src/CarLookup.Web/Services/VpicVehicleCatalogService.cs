using System.Net.Http.Json;
using System.Text.Json;
using CarLookup.Web.Models;
using CarLookup.Web.Models.Vpic;

namespace CarLookup.Web.Services;

/// <summary>
/// Talks to the public NHTSA vPIC API over HTTP and maps its wire contracts onto
/// the application's own models.
/// </summary>
public sealed class VpicVehicleCatalogService : IVehicleCatalogService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger<VpicVehicleCatalogService> _logger;

    public VpicVehicleCatalogService(HttpClient httpClient, ILogger<VpicVehicleCatalogService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Make>> GetMakesAsync(CancellationToken cancellationToken = default)
    {
        var results = await GetAsync<VpicMake>("getallmakes?format=json", cancellationToken);

        return results
            .Where(m => !string.IsNullOrWhiteSpace(m.MakeName))
            .Select(m => new Make(m.MakeId, ToTitleCase(m.MakeName)))
            .OrderBy(m => m.MakeName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<VehicleType>> GetVehicleTypesAsync(
        int makeId,
        CancellationToken cancellationToken = default)
    {
        var results = await GetAsync<VpicVehicleType>(
            $"GetVehicleTypesForMakeId/{makeId}?format=json",
            cancellationToken);

        return results
            .Select(t => new VehicleType(t.VehicleTypeId, t.VehicleTypeName))
            .OrderBy(t => t.VehicleTypeName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<VehicleModel>> GetModelsAsync(
        int makeId,
        int year,
        string? vehicleType = null,
        CancellationToken cancellationToken = default)
    {
        // vPIC exposes a dedicated route when the caller also filters by vehicle type,
        // which is cheaper than pulling every model and filtering locally.
        var path = string.IsNullOrWhiteSpace(vehicleType)
            ? $"GetModelsForMakeIdYear/makeId/{makeId}/modelyear/{year}?format=json"
            : $"GetModelsForMakeIdYear/makeId/{makeId}/modelyear/{year}/vehicletype/{Uri.EscapeDataString(vehicleType)}?format=json";

        var results = await GetAsync<VpicModel>(path, cancellationToken);

        return results
            .Select(m => new VehicleModel(m.ModelId, m.ModelName, m.VehicleTypeName))
            .OrderBy(m => m.ModelName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<IReadOnlyList<T>> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Calling vPIC: {Path}", path);

        try
        {
            using var response = await _httpClient.GetAsync(path, cancellationToken);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<VpicResponse<T>>(
                SerializerOptions,
                cancellationToken);

            return payload?.Results ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException
                                   && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "vPIC request failed: {Path}", path);
            throw new VpicUnavailableException($"The vehicle data service did not answer the request '{path}'.", ex);
        }
    }

    /// <summary>
    /// vPIC returns make names in upper case ("HONDA"). Title-casing them keeps the UI readable
    /// while leaving short all-caps names such as "BMW" or "GMC" alone.
    /// </summary>
    private static string ToTitleCase(string value)
    {
        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return string.Join(' ', words.Select(word =>
            word.Length <= 3 || word.Any(char.IsDigit)
                ? word
                : char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()));
    }
}

/// <summary>Raised when the upstream vPIC API cannot be reached or returns something unusable.</summary>
public sealed class VpicUnavailableException : Exception
{
    public VpicUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
