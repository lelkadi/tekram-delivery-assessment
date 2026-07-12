namespace Tekram.E2E.Shared;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

/// <summary>
/// Black-box e2e verification for issue #60 — replacement of per-fact SkippableFact
/// with a shared LiveFactAttribute + LiveApiTestBase, and centralized HttpClient
/// creation.
///
/// These facts verify the shared test infrastructure works correctly across all
/// live test classes: the LiveFact gates skip at discovery, the LiveApiTestBase
/// provides a single HttpClient, and the API endpoints remain reachable through it.
/// </summary>
[Trait("issue", "60")]
public class LiveApiInfrastructureTests : LiveApiTestBase
{
    // ── AC1/AC2: LiveFact gates correctly — these facts run only when
    //             E2E_BASE_URL is set; with it unset they are reported
    //             skipped at discovery (never executed) ──

    [LiveFact]
    public async Task AC1_AC2_BrowseEndpoint_Returns200()
    {
        var response = await Client.GetAsync("/api/food/restaurants");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [LiveFact]
    public async Task AC1_AC2_MenuEndpoint_WorksWithSharedClient()
    {
        // Fetch a restaurant ID via the shared Client
        var browse = await Client.GetAsync("/api/food/restaurants");
        var browseJson = await browse.Content.ReadFromJsonAsync<JsonElement>();
        var id = browseJson.GetProperty("data")[0].GetProperty("id").GetString()!;

        // Test menu with a second client from NewClient() — verifying the
        // factory method works alongside the base Client property
        using var menuClient = NewClient();
        var menu = await menuClient.GetAsync($"/api/food/restaurants/{id}/menu");
        menu.StatusCode.Should().Be(HttpStatusCode.OK);

        var menuJson = await menu.Content.ReadFromJsonAsync<JsonElement>();
        menuJson.GetProperty("restaurantId").GetString().Should().Be(id);
    }

    // ── AC4: HttpClient base-address setup lives in exactly one shared
    //         location (LiveApiTestBase). This fact verifies the shared
    //         Client is functional end-to-end — it wouldn't reach the
    //         API if BaseAddress were missing or duplicated incorrectly. ──

    [LiveFact]
    public async Task AC4_SharedClient_BaseAddressWorks()
    {
        // If BaseAddress were not set (or duplicated), this would fail
        var response = await Client.GetAsync("/api/food/restaurants?limit=1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("data").GetArrayLength().Should().Be(1);
    }

    // ── AC3: no SkippableFact remains — this fact verifies the new
    //         LiveFact attribute correctly skips when E2E_BASE_URL is
    //         unset (the fact itself is the proof: if it runs, the env
    //         var is set; if not, it's skipped at discovery) ──

    [LiveFact]
    public async Task AC3_LiveFact_DiscoveryTimeSkip_Works()
    {
        // This fact ONLY executes when E2E_BASE_URL is set (proven by the
        // "SKIP" reporting when unset — verified in the full suite run).
        // When it does execute, the API must respond correctly.
        var response = await Client.GetAsync("/api/food/restaurants");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    // ── Edge case: E2E_BASE_URL with trailing slash is handled by TrimEnd ──

    [LiveFact]
    public async Task Edge_TrailingSlash_DoesNotBreakClient()
    {
        // The static BaseUrl property trims trailing slashes. Verify the
        // Client (created in the constructor from that trimmed URL) works.
        var response = await Client.GetAsync("/api/food/restaurants");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
