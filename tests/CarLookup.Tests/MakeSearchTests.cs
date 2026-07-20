using CarLookup.Web.Models;
using CarLookup.Web.Services;

namespace CarLookup.Tests;

public class MakeSearchTests
{
    private static readonly IReadOnlyList<Make> Makes =
    [
        new(1, "Honda"),
        new(2, "Sundiro Honda Motorcycle CO. LTD"),
        new(3, "Honda Performance Development"),
        new(4, "BMW"),
        new(5, "Toyota")
    ];

    [Fact]
    public void Ranks_exact_match_first_then_prefix_then_contains()
    {
        var results = MakeSearch.Filter(Makes, "honda");

        Assert.Equal(
            ["Honda", "Honda Performance Development", "Sundiro Honda Motorcycle CO. LTD"],
            results.Select(m => m.MakeName));
    }

    [Fact]
    public void Matching_is_case_insensitive_and_ignores_surrounding_whitespace()
    {
        var results = MakeSearch.Filter(Makes, "  bMw  ");

        Assert.Equal("BMW", Assert.Single(results).MakeName);
    }

    [Fact]
    public void Returns_the_head_of_the_list_when_no_query_is_supplied()
    {
        var results = MakeSearch.Filter(Makes, query: null, limit: 2);

        Assert.Equal(2, results.Count);
        Assert.Equal("Honda", results[0].MakeName);
    }

    [Fact]
    public void Returns_nothing_when_nothing_matches()
    {
        Assert.Empty(MakeSearch.Filter(Makes, "definitely-not-a-make"));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(500, MakeSearch.MaxLimit)]
    public void Clamps_the_limit_into_a_sane_range(int requested, int expectedMax)
    {
        var manyMakes = Enumerable.Range(1, 300).Select(i => new Make(i, $"Make {i}")).ToList();

        var results = MakeSearch.Filter(manyMakes, "Make", requested);

        Assert.Equal(expectedMax, results.Count);
    }
}
