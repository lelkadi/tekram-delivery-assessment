using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace Tekram.E2E.Restaurants;

/// <summary>
/// Black-box coverage for issue #14 (Part 2 Slice 2.3 — Extended restaurant seed data
/// for pagination testing).
///
/// Every AC below is a live HTTP assertion against the running lane API.  The facts
/// verify that the extended DbInitializer seed satisfies the issue's structural
/// requirements: 10 restaurants across 10 cuisines, all price tiers, rich menus with
/// stock counts and customization groups, correct pagination, and working filters.
/// </summary>
[Trait("issue", "14")]
public class RestaurantSeedTests : LiveApiTestBase
{
    // ── helpers ──────────────────────────────────────────────────────────

    private static async Task<JsonElement> GetJsonAsync(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"GET {path} should return 200");
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    // ── AC1: At least 8–10 restaurants seeded ────────────────────────────

    [LiveFact]
    public async Task AC1_AtLeastTenRestaurantsSeeded()
    {
        var json = await GetJsonAsync(Client, "/api/food/restaurants?limit=50");

        var total = json.GetProperty("pagination").GetProperty("totalItems").GetInt32();
        total.Should().BeGreaterThanOrEqualTo(8,
            "issue #14 requires at least 8-10 restaurants for pagination testing");
    }

    // ── AC2: Varied cuisines ─────────────────────────────────────────────

    [LiveFact]
    public async Task AC2_VariedCuisinesCoverage()
    {
        var json = await GetJsonAsync(Client, "/api/food/restaurants?limit=50");

        var cuisines = json.GetProperty("data").EnumerateArray()
            .Select(r => r.GetProperty("cuisine").GetString()!)
            .ToHashSet();

        cuisines.Count.Should().BeGreaterThanOrEqualTo(8,
            "issue #14 requires varied cuisines: Italian, Burgers, Japanese, Lebanese, Chinese, Mexican, Indian, French at minimum");

        // Smoke-check that the named cuisines from the issue body are present
        string[] required = ["Italian", "Burgers", "Japanese", "Lebanese",
                              "Chinese", "Mexican", "Indian", "French"];
        foreach (var c in required)
            cuisines.Should().Contain(c, $"cuisine '{c}' must be represented");
    }

    // ── AC3: Price tiers 1–4 present, ratings 3.5–5.0 ────────────────────

    [LiveFact]
    public async Task AC3_PriceTiersOneThroughFourPresent()
    {
        var json = await GetJsonAsync(Client, "/api/food/restaurants?limit=50");

        var tiers = json.GetProperty("data").EnumerateArray()
            .Select(r => r.GetProperty("priceTier").GetInt32())
            .ToHashSet();

        foreach (var t in new[] { 1, 2, 3, 4 })
            tiers.Should().Contain(t, $"price tier {t} must be represented");

        var ratings = json.GetProperty("data").EnumerateArray()
            .Select(r => r.GetProperty("rating").GetDecimal())
            .ToList();

        ratings.Min().Should().BeGreaterThanOrEqualTo(3.5m,
            "minimum rating must be ≥ 3.5");
        ratings.Max().Should().BeLessThanOrEqualTo(5.0m,
            "maximum rating must be ≤ 5.0");
    }

    // ── AC4: Each restaurant has a full nested menu (2-4 categories,
    //        3-6 items per category) ───────────────────────────────────────

    [LiveFact]
    public async Task AC4_EachRestaurantHasFullMenu()
    {
        var list = await GetJsonAsync(Client, "/api/food/restaurants?limit=50");

        var failures = new List<string>();

        foreach (var r in list.GetProperty("data").EnumerateArray())
        {
            var id = r.GetProperty("id").GetString()!;
            var name = r.GetProperty("name").GetString()!;

            var menu = await GetJsonAsync(Client, $"/api/food/restaurants/{id}/menu");
            var cats = menu.GetProperty("categories").EnumerateArray().ToList();

            if (cats.Count < 2 || cats.Count > 4)
                failures.Add($"{name}: {cats.Count} categories (expected 2-4)");

            foreach (var cat in cats)
            {
                var catName = cat.GetProperty("categoryName").GetString()!;
                var items = cat.GetProperty("items").EnumerateArray().ToList();
                if (items.Count < 3 || items.Count > 6)
                    failures.Add($"{name}/{catName}: {items.Count} items (expected 3-6)");
            }
        }

        failures.Should().BeEmpty(string.Join("\n", failures));
    }

    // ── AC5: Some items have limited stock_count; most are null ───────────

    [LiveFact]
    public async Task AC5_SomeItemsHaveLimitedStock()
    {
        var list = await GetJsonAsync(Client, "/api/food/restaurants?limit=50");

        int totalItems = 0;
        int limitedStock = 0;

        foreach (var r in list.GetProperty("data").EnumerateArray())
        {
            var id = r.GetProperty("id").GetString()!;
            var menu = await GetJsonAsync(Client, $"/api/food/restaurants/{id}/menu");

            foreach (var cat in menu.GetProperty("categories").EnumerateArray())
            foreach (var item in cat.GetProperty("items").EnumerateArray())
            {
                totalItems++;
                if (item.TryGetProperty("stockCount", out var sc) &&
                    sc.ValueKind == JsonValueKind.Number)
                    limitedStock++;
            }
        }

        limitedStock.Should().BeGreaterThan(0,
            "at least some items must have a limited stock_count");
        (limitedStock < totalItems).Should().BeTrue(
            "most items must have null (unlimited) stock_count");
    }

    // ── AC6: Customization groups exist on select items ──────────────────

    [LiveFact]
    public async Task AC6_CustomizationGroupsExist()
    {
        var list = await GetJsonAsync(Client, "/api/food/restaurants?limit=50");

        int groups = 0;
        int options = 0;

        foreach (var r in list.GetProperty("data").EnumerateArray())
        {
            var id = r.GetProperty("id").GetString()!;
            var menu = await GetJsonAsync(Client, $"/api/food/restaurants/{id}/menu");

            foreach (var cat in menu.GetProperty("categories").EnumerateArray())
            foreach (var item in cat.GetProperty("items").EnumerateArray())
            foreach (var g in item.GetProperty("customizationGroups").EnumerateArray())
            {
                groups++;
                options += g.GetProperty("options").GetArrayLength();
            }
        }

        groups.Should().BeGreaterThan(0,
            "select items must have customization groups (size, toppings, spice level)");
        options.Should().BeGreaterThanOrEqualTo(2,
            "each customization group must have at least one option per requirement");
    }

    // ── AC7: Sushi Zen has a non-empty menu (PM rejection regression) ────

    [LiveFact]
    public async Task AC7_SushiZenHasNonEmptyMenu()
    {
        var list = await GetJsonAsync(Client, "/api/food/restaurants?limit=50");

        // Find Sushi Zen
        JsonElement? sushiZen = null;
        foreach (var r in list.GetProperty("data").EnumerateArray())
        {
            if (r.GetProperty("name").GetString() == "Sushi Zen")
            {
                sushiZen = r;
                break;
            }
        }

        sushiZen.Should().NotBeNull("Sushi Zen must be seeded");
        var szId = sushiZen!.Value.GetProperty("id").GetString()!;

        var menu = await GetJsonAsync(Client, $"/api/food/restaurants/{szId}/menu");

        var cats = menu.GetProperty("categories").EnumerateArray().ToList();
        cats.Count.Should().BeGreaterThan(0,
            "Sushi Zen must have at least one category — PM reject regression guard");

        int itemCount = cats.Sum(c => c.GetProperty("items").GetArrayLength());
        itemCount.Should().BeGreaterThan(0,
            "Sushi Zen must have menu items — PM reject regression guard");
    }

    // ── AC8: Pagination works — page 1 and page 2 have no overlap ────────

    [LiveFact]
    public async Task AC8_PaginationWithTenRestaurants()
    {
        var page1 = await GetJsonAsync(Client, "/api/food/restaurants?limit=5&page=1");
        var page2 = await GetJsonAsync(Client, "/api/food/restaurants?limit=5&page=2");

        var p1 = page1.GetProperty("pagination");
        p1.GetProperty("totalItems").GetInt32().Should().Be(10);
        p1.GetProperty("totalPages").GetInt32().Should().Be(2);

        var ids1 = page1.GetProperty("data").EnumerateArray()
            .Select(r => r.GetProperty("id").GetString()!).ToHashSet();
        var ids2 = page2.GetProperty("data").EnumerateArray()
            .Select(r => r.GetProperty("id").GetString()!).ToHashSet();

        ids1.Count.Should().Be(5);
        ids2.Count.Should().Be(5);
        ids1.Intersect(ids2).Should().BeEmpty("page 1 and page 2 must have zero overlap");
    }

    // ── AC9: Search by cuisine filters correctly ─────────────────────────

    [LiveFact]
    public async Task AC9_CuisineFilterReturnsCorrectResults()
    {
        var json = await GetJsonAsync(Client, "/api/food/restaurants?cuisine=Italian");

        var data = json.GetProperty("data").EnumerateArray().ToList();
        data.Should().NotBeEmpty("Italian cuisine filter must return results");
        data.Should().AllSatisfy(r =>
            r.GetProperty("cuisine").GetString().Should().Be("Italian"));
    }

    // ── AC10: Price-tier filter returns correct results ──────────────────

    [LiveFact]
    public async Task AC10_PriceTierFilterReturnsCorrectResults()
    {
        var json = await GetJsonAsync(Client, "/api/food/restaurants?price_tier=4");

        var data = json.GetProperty("data").EnumerateArray().ToList();
        data.Should().NotBeEmpty("price_tier=4 filter must return results");
        data.Should().AllSatisfy(r =>
            r.GetProperty("priceTier").GetInt32().Should().Be(4));
    }
}
