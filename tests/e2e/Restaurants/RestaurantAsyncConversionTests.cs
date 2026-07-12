namespace Tekram.E2E.Restaurants;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

/// <summary>
/// Black-box e2e verification for issue #59 — conversion of RestaurantEndpointTests
/// from sync (.GetAwaiter().GetResult()) to fully async (async Task + await).
///
/// These facts act as a regression gate: they verify the restaurant browse and menu
/// endpoints still respond correctly after the internal async plumbing changed.
/// The async conversion is an implementation detail — the API contract must be
/// identical, so these tests exercise the same endpoints as the original suite
/// with a few additional stress/concurrency checks.
/// </summary>
[Trait("issue", "59")]
public class RestaurantAsyncConversionTests
{
    private static readonly string? BaseUrl = Environment.GetEnvironmentVariable("E2E_BASE_URL");

    private static HttpClient CreateClient()
    {
        if (BaseUrl is null) throw new InvalidOperationException("E2E_BASE_URL is not set");
        return new HttpClient { BaseAddress = new Uri(BaseUrl) };
    }

    private static bool ShouldSkip() => BaseUrl is null;

    // ── AC3 regression: browse still returns valid envelope ──

    [SkippableFact]
    public void AC3_Browse_Returns200_Envelope()
    {
        Skip.If(ShouldSkip());
        var client = CreateClient();

        var response = client.GetAsync("/api/food/restaurants").GetAwaiter().GetResult();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = response.Content.ReadFromJsonAsync<JsonElement>().GetAwaiter().GetResult();
        json.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Array);
        json.GetProperty("pagination").GetProperty("totalItems").GetInt32().Should().BeGreaterThan(0);
    }

    // ── AC3 regression: menu still works ──

    [SkippableFact]
    public void AC3_Menu_ReturnsValidStructure()
    {
        Skip.If(ShouldSkip());
        var client = CreateClient();

        // Pick the first restaurant
        var browse = client.GetAsync("/api/food/restaurants").GetAwaiter().GetResult();
        var browseJson = browse.Content.ReadFromJsonAsync<JsonElement>().GetAwaiter().GetResult();
        var id = browseJson.GetProperty("data")[0].GetProperty("id").GetString()!;

        // Fetch its menu
        var menu = client.GetAsync($"/api/food/restaurants/{id}/menu").GetAwaiter().GetResult();
        menu.StatusCode.Should().Be(HttpStatusCode.OK);

        var menuJson = menu.Content.ReadFromJsonAsync<JsonElement>().GetAwaiter().GetResult();
        menuJson.GetProperty("restaurantId").GetString().Should().Be(id);
        menuJson.GetProperty("categories").ValueKind.Should().Be(JsonValueKind.Array);
    }

    // ── AC3: concurrent requests do not deadlock (async plumbing sanity check) ──

    [SkippableFact]
    public void AC3_ConcurrentRequests_AllSucceed()
    {
        Skip.If(ShouldSkip());
        var client = CreateClient();

        // Fire 5 concurrent GETs — async deadlocks (if any) would surface here
        var tasks = Enumerable.Range(0, 5).Select(_ =>
            client.GetAsync("/api/food/restaurants")).ToArray();

        Task.WaitAll(tasks);

        foreach (var t in tasks)
        {
            t.Result.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    // ── AC3: pagination edge — valid limit works ──

    [SkippableFact]
    public void AC3_Pagination_ValidLimit_Works()
    {
        Skip.If(ShouldSkip());
        var client = CreateClient();

        var response = client.GetAsync("/api/food/restaurants?limit=3").GetAwaiter().GetResult();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = response.Content.ReadFromJsonAsync<JsonElement>().GetAwaiter().GetResult();
        json.GetProperty("pagination").GetProperty("limit").GetInt32().Should().Be(3);
        json.GetProperty("data").GetArrayLength().Should().BeLessThanOrEqualTo(3);
    }

    // ── AC3: invalid params still return 422 after async conversion ──

    [SkippableFact]
    public void AC3_InvalidLimit_Returns422()
    {
        Skip.If(ShouldSkip());
        var client = CreateClient();

        var response = client.GetAsync("/api/food/restaurants?limit=-1").GetAwaiter().GetResult();
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
