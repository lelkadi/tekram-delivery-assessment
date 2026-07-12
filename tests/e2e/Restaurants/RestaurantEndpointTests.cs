namespace Tekram.E2E.Restaurants;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

/// <summary>
/// Black-box e2e tests for restaurant browse and menu endpoints.
/// Verifies #13's handlers, infrastructure, and presentation layer
/// against the live API. Requires E2E_BASE_URL.
/// </summary>
[Trait("issue", "13")]
public class RestaurantEndpointTests
{
    private static readonly string? BaseUrl = Environment.GetEnvironmentVariable("E2E_BASE_URL");

    private static HttpClient CreateClient()
    {
        if (BaseUrl is null) throw new InvalidOperationException("E2E_BASE_URL is not set");
        return new HttpClient { BaseAddress = new Uri(BaseUrl) };
    }

    private static bool ShouldSkip() => BaseUrl is null;

    private static async Task<JsonElement> GetJson(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<JsonElement>())!;

    // ====================================================================
    // AC1: GET /api/food/restaurants — browse with filters
    // ====================================================================

    [SkippableFact]
    public async Task AC1_DefaultBrowse_Returns200_Envelope()
    {
        Skip.If(ShouldSkip());
        var client = CreateClient();
        var response = await client.GetAsync("/api/food/restaurants");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await GetJson(response);
        json.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Array);
        json.GetProperty("pagination").GetProperty("currentPage").GetInt32().Should().Be(1);
        json.GetProperty("pagination").GetProperty("totalItems").GetInt32().Should().BeGreaterThan(0);
    }

    [SkippableFact]
    public async Task AC1_RatingDescending_DefaultSort()
    {
        Skip.If(ShouldSkip());
        var client = CreateClient();
        var response = await client.GetAsync("/api/food/restaurants");
        var json = await GetJson(response);
        var ratings = json.GetProperty("data").EnumerateArray()
            .Select(r => r.GetProperty("rating").GetDecimal()).ToList();
        ratings.Should().BeInDescendingOrder("default sort is rating descending");
    }

    [SkippableFact]
    public async Task AC1_Search_ILIKE_CaseInsensitivePartial()
    {
        Skip.If(ShouldSkip());
        var client = CreateClient();
        var response = await client.GetAsync("/api/food/restaurants?search=sushi");
        var json = await GetJson(response);
        var names = json.GetProperty("data").EnumerateArray()
            .Select(r => r.GetProperty("name").GetString()).ToList();
        names.Should().Contain(n => n!.Contains("Sushi", StringComparison.OrdinalIgnoreCase));
    }

    [SkippableFact]
    public async Task AC1_CuisineFilter_ExactMatch()
    {
        Skip.If(ShouldSkip());
        var client = CreateClient();
        var response = await client.GetAsync("/api/food/restaurants?cuisine=Italian");
        var json = await GetJson(response);
        var data = json.GetProperty("data").EnumerateArray().ToList();
        data.Should().NotBeEmpty();
        data.Should().OnlyContain(r => r.GetProperty("cuisine").GetString() == "Italian");
    }

    [SkippableFact]
    public async Task AC1_PriceTier_SnakeCase_FiltersCorrectly()
    {
        Skip.If(ShouldSkip());
        var client = CreateClient();
        // price_tier=2 (snake_case) — the documented API contract
        var response = await client.GetAsync("/api/food/restaurants?price_tier=2");
        var json = await GetJson(response);
        var data = json.GetProperty("data").EnumerateArray().ToList();
        data.Should().NotBeEmpty("there must be restaurants with price tier 2");
        data.Should().OnlyContain(r => r.GetProperty("priceTier").GetInt32() == 2);
    }

    [SkippableFact]
    public async Task AC1_CombinedFilters_AndLogic()
    {
        Skip.If(ShouldSkip());
        var client = CreateClient();
        var response = await client.GetAsync("/api/food/restaurants?search=zen&cuisine=Japanese");
        var json = await GetJson(response);
        var data = json.GetProperty("data").EnumerateArray().ToList();
        data.Should().NotBeEmpty();
        data.Should().OnlyContain(r => r.GetProperty("name").GetString()!.Contains("Zen"));
    }

    // ====================================================================
    // AC2: Pagination deterministic ordering (ThenBy Id fix)
    // ====================================================================

    [SkippableFact]
    public async Task AC2_Pagination_NoOverlapAcrossPages()
    {
        Skip.If(ShouldSkip());
        var client = CreateClient();

        var p1Resp = await client.GetAsync("/api/food/restaurants?limit=3&page=1");
        var p1Json = await GetJson(p1Resp);
        var p1Ids = p1Json.GetProperty("data").EnumerateArray()
            .Select(r => r.GetProperty("id").GetGuid()).ToList();

        var p2Resp = await client.GetAsync("/api/food/restaurants?limit=3&page=2");
        var p2Json = await GetJson(p2Resp);
        var p2Ids = p2Json.GetProperty("data").EnumerateArray()
            .Select(r => r.GetProperty("id").GetGuid()).ToList();

        p1Ids.Should().NotBeEmpty();
        p2Ids.Should().NotBeEmpty();
        p1Ids.Should().NotIntersectWith(p2Ids,
            "page 1 and page 2 must not overlap (deterministic secondary sort on Id)");
    }

    [SkippableFact]
    public async Task AC2_Pagination_TotalPages_Correct()
    {
        Skip.If(ShouldSkip());
        var client = CreateClient();
        var response = await client.GetAsync("/api/food/restaurants?limit=4");
        var json = await GetJson(response);
        var p = json.GetProperty("pagination");
        p.GetProperty("totalItems").GetInt32().Should().Be(10);
        p.GetProperty("totalPages").GetInt32().Should().Be(3); // ceil(10/4)
    }

    // ====================================================================
    // AC3: Validation
    // ====================================================================

    [SkippableFact]
    public async Task AC3_InvalidPage_Returns422()
    {
        Skip.If(ShouldSkip());
        var client = CreateClient();
        var response = await client.GetAsync("/api/food/restaurants?page=0");
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [SkippableFact]
    public async Task AC3_InvalidLimit_Returns422()
    {
        Skip.If(ShouldSkip());
        var client = CreateClient();
        var response = await client.GetAsync("/api/food/restaurants?limit=51");
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ====================================================================
    // AC4: GET /api/food/restaurants/{id}/menu
    // ====================================================================

    [SkippableFact]
    public async Task AC4_ValidMenu_NestedStructure()
    {
        Skip.If(ShouldSkip());
        var client = CreateClient();
        // Get first restaurant
        var list = await client.GetAsync("/api/food/restaurants?limit=1");
        var listJson = await GetJson(list);
        var id = listJson.GetProperty("data").EnumerateArray().First().GetProperty("id").GetGuid();

        var response = await client.GetAsync($"/api/food/restaurants/{id}/menu");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await GetJson(response);

        json.GetProperty("restaurantId").GetGuid().Should().Be(id);
        var categories = json.GetProperty("categories").EnumerateArray().ToList();
        categories.Should().NotBeEmpty();

        foreach (var cat in categories)
        {
            cat.GetProperty("categoryId").ValueKind.Should().Be(JsonValueKind.String);
            cat.GetProperty("categoryName").GetString().Should().NotBeNullOrEmpty();
            cat.GetProperty("displayOrder").ValueKind.Should().Be(JsonValueKind.Number);

            var items = cat.GetProperty("items").EnumerateArray().ToList();
            items.Should().NotBeEmpty();

            foreach (var item in items)
            {
                item.GetProperty("id").ValueKind.Should().Be(JsonValueKind.String);
                item.GetProperty("name").GetString().Should().NotBeNullOrEmpty();
                item.GetProperty("priceUsd").ValueKind.Should().Be(JsonValueKind.Number);
                item.GetProperty("isAvailable").ValueKind.Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);
            }
        }
    }

    [SkippableFact]
    public async Task AC4_Menu_404_UnknownRestaurant()
    {
        Skip.If(ShouldSkip());
        var client = CreateClient();
        var response = await client.GetAsync($"/api/food/restaurants/{Guid.NewGuid()}/menu");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var json = await GetJson(response);
        json.GetProperty("error").GetString().Should().Be("not_found");
    }

    [SkippableFact]
    public async Task AC4_Menu_isAvailable_NonNull()
    {
        Skip.If(ShouldSkip());
        var client = CreateClient();
        var list = await client.GetAsync("/api/food/restaurants?limit=1");
        var listJson = await GetJson(list);
        var id = listJson.GetProperty("data").EnumerateArray().First().GetProperty("id").GetGuid();

        var response = await client.GetAsync($"/api/food/restaurants/{id}/menu");
        var json = await GetJson(response);
        var items = json.GetProperty("categories").EnumerateArray()
            .SelectMany(c => c.GetProperty("items").EnumerateArray());

        foreach (var item in items)
        {
            var kind = item.GetProperty("isAvailable").ValueKind;
            (kind == JsonValueKind.True || kind == JsonValueKind.False).Should().BeTrue(
                "isAvailable must be a non-null boolean");
        }
    }
}
