namespace CarLookup.Web.Models;

/// <summary>Backing model for the lookup page.</summary>
public sealed class LookupViewModel
{
    /// <summary>Selectable model years, newest first. Rendered server-side so the year list
    /// is present even before any script runs.</summary>
    public required IReadOnlyList<int> Years { get; init; }
}
