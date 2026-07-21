namespace CarLookup.Api.Models;

/// <summary>A vehicle manufacturer.</summary>
public sealed record Make(int MakeId, string MakeName);

/// <summary>A category of vehicle a make produces, e.g. "Passenger Car".</summary>
public sealed record VehicleType(int VehicleTypeId, string VehicleTypeName);

/// <summary>A model produced by a make in a given year.</summary>
public sealed record VehicleModel(int ModelId, string ModelName, string? VehicleTypeName);
