using CarLookup.Api.Models;

namespace CarLookup.Api.Services;

/// <summary>
/// Ranks makes for the type-ahead. vPIC returns ~12,000 makes, far too many to push into a
/// dropdown, so the browser sends what the user typed and gets back a short, ranked list.
/// </summary>
public static class MakeSearch
{
    public const int DefaultLimit = 25;
    public const int MaxLimit = 100;

    public static IReadOnlyList<Make> Filter(IReadOnlyList<Make> makes, string? query, int limit = DefaultLimit)
    {
        limit = Math.Clamp(limit, 1, MaxLimit);

        if (string.IsNullOrWhiteSpace(query))
        {
            return makes.Take(limit).ToList();
        }

        var term = query.Trim();

        return makes
            .Select(make => (Make: make, Rank: Rank(make.MakeName, term)))
            .Where(candidate => candidate.Rank < int.MaxValue)
            .OrderBy(candidate => candidate.Rank)
            .ThenBy(candidate => candidate.Make.MakeName.Length)
            .ThenBy(candidate => candidate.Make.MakeName, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .Select(candidate => candidate.Make)
            .ToList();
    }

    /// <summary>
    /// Exact match beats prefix match beats "contains", so typing "bmw" surfaces BMW itself
    /// ahead of the many coachbuilders with "BMW" somewhere in their name.
    /// </summary>
    private static int Rank(string name, string term)
    {
        if (name.Equals(term, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (name.StartsWith(term, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return name.Contains(term, StringComparison.OrdinalIgnoreCase) ? 2 : int.MaxValue;
    }
}
