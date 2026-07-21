using System.ComponentModel.DataAnnotations;

namespace CarLookup.Api.Services;

/// <summary>
/// Configuration for the NHTSA vPIC integration, bound from the "Vpic" configuration section.
/// </summary>
public sealed class VpicOptions
{
    public const string SectionName = "Vpic";

    /// <summary>Base address of the vPIC vehicles API.</summary>
    [Required]
    public string BaseUrl { get; set; } = "https://vpic.nhtsa.dot.gov/api/vehicles/";

    /// <summary>How long a single upstream call may take before it is abandoned.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Lifetime of the cached make list. The list changes rarely, so it is cached aggressively:
    /// it is ~12,000 rows and is needed on every page load.
    /// </summary>
    public TimeSpan MakesCacheDuration { get; set; } = TimeSpan.FromHours(24);

    /// <summary>Lifetime of cached per-make lookups (vehicle types and models).</summary>
    public TimeSpan LookupCacheDuration { get; set; } = TimeSpan.FromHours(1);
}
