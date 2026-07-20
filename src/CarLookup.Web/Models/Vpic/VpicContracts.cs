using System.Text.Json.Serialization;

namespace CarLookup.Web.Models.Vpic;

/// <summary>
/// Envelope that every vPIC endpoint wraps its payload in.
/// </summary>
public sealed record VpicResponse<T>
{
    public int Count { get; init; }

    public string? Message { get; init; }

    public string? SearchCriteria { get; init; }

    public IReadOnlyList<T> Results { get; init; } = [];
}

/// <summary>Row returned by <c>GetAllMakes</c>.</summary>
public sealed record VpicMake
{
    [JsonPropertyName("Make_ID")]
    public int MakeId { get; init; }

    [JsonPropertyName("Make_Name")]
    public string MakeName { get; init; } = string.Empty;
}

/// <summary>Row returned by <c>GetVehicleTypesForMakeId</c>.</summary>
public sealed record VpicVehicleType
{
    public int VehicleTypeId { get; init; }

    public string VehicleTypeName { get; init; } = string.Empty;
}

/// <summary>Row returned by <c>GetModelsForMakeIdYear</c>.</summary>
public sealed record VpicModel
{
    [JsonPropertyName("Make_ID")]
    public int MakeId { get; init; }

    [JsonPropertyName("Make_Name")]
    public string MakeName { get; init; } = string.Empty;

    [JsonPropertyName("Model_ID")]
    public int ModelId { get; init; }

    [JsonPropertyName("Model_Name")]
    public string ModelName { get; init; } = string.Empty;

    /// <summary>Only populated when the vehicle-type filtered endpoint is used.</summary>
    public string? VehicleTypeName { get; init; }
}
