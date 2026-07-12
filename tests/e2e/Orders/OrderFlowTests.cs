namespace Tekram.E2E.Orders;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

/// <summary>
/// Black-box e2e flow for issue #62 — the core ordering happy path proven against the
/// running lane API: browse restaurants, read a menu, place an authenticated order,
/// and assert a successful order response.
///
/// KNOWN RED (at authoring time): the order placement step fails with 401 because of
/// the #16-scope claim-type mismatch — the JWT carries the user id as a short "sub"
/// claim but OrderEndpoints extracts ClaimTypes.NameIdentifier (see the documented
/// known-bug fact in OrdersCouponSeedTests.AC3). This fact is the machine-checkable
/// acceptance bar for the fixed behavior and must turn green when #16's fix merges;
/// at that point OrdersCouponSeedTests.AC3 (which pins the buggy 401) goes red and
/// must be updated per its own in-code instruction.
/// </summary>
[Trait("issue", "62")]
public class OrderFlowTests : LiveApiTestBase
{
    private static async Task<JsonElement> GetJson(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<JsonElement>())!;

    [LiveFact]
    public async Task AC1_BrowseMenuThenOrder_ReturnsCreatedOrderWithTotals()
    {
        // Establish auth within the flow — registration returns the JWT directly,
        // avoiding the per-identifier login rate limiter.
        var email = $"e2e-orderflow-{Guid.NewGuid():N}@test.com";
        var phone = $"+961{70_000_000 + Random.Shared.Next(0, 9_999_999)}";
        var regResp = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "E2E Order Flow",
            email,
            phone,
            password = "Test123!",
            role = "customer",
        });
        regResp.StatusCode.Should().Be(HttpStatusCode.Created, "registration must succeed");
        var token = (await GetJson(regResp)).GetProperty("token").GetString()!;

        // Browse restaurants.
        var listResp = await Client.GetAsync("/api/food/restaurants?limit=1");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK, "restaurant browse must succeed");
        var listJson = await GetJson(listResp);
        var restaurantId = listJson.GetProperty("data")[0].GetProperty("id").GetString()!;

        // View its menu.
        var menuResp = await Client.GetAsync($"/api/food/restaurants/{restaurantId}/menu");
        menuResp.StatusCode.Should().Be(HttpStatusCode.OK, "menu view must succeed");
        var menuJson = await GetJson(menuResp);
        var menuItemId = menuJson.GetProperty("categories")[0]
            .GetProperty("items")[0].GetProperty("id").GetString()!;

        // Place the order with the JWT.
        using var authClient = NewClient();
        authClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var orderResp = await authClient.PostAsJsonAsync("/api/food/orders", new
        {
            restaurantId,
            items = new[]
            {
                new { menuItemId, quantity = 1, customizationChoices = (object[]?)null },
            },
            deliveryAddress = "Test Street, Beirut",
            paymentMethod = "COD",
            couponCode = (string?)null,
            specialInstructions = (string?)null,
        });

        orderResp.StatusCode.Should().Be(HttpStatusCode.Created,
            "an authenticated, valid order must be created (currently red: #16 claim-type " +
            "mismatch returns 401 for every valid JWT — this fact is the acceptance bar " +
            "for the fix)");

        var order = await GetJson(orderResp);
        order.GetProperty("bookingId").GetGuid().Should().NotBeEmpty("order must return its id");
        order.GetProperty("status").GetString().Should().NotBeNullOrEmpty();
        var totals = order.GetProperty("totals");
        totals.GetProperty("subtotalUsd").GetDecimal().Should().BeGreaterThan(0);
        totals.GetProperty("totalUsd").GetDecimal().Should().BeGreaterThan(0);
    }
}
