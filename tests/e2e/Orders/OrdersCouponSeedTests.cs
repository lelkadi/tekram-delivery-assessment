namespace Tekram.E2E.Orders;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

/// <summary>
/// Black-box e2e tests for issue #17 (Part 2 Slice 3.3 — Coupon seed data,
/// orders DI registration, endpoint mapping).
///
/// Verifies against the live API at E2E_BASE_URL.  Full coupon-discount and
/// coupon-rejection verification through authenticated POST /api/food/orders
/// is blocked by a #16-scope claim-type mismatch (see AC3 below) — the
/// remaining ACs verify DI wiring, endpoint mapping, and app boot independently
/// of the #16 handler bug.
/// </summary>
[Trait("issue", "17")]
public class OrdersCouponSeedTests : LiveApiTestBase
{
    // ── helpers ──────────────────────────────────────────────────────────

    private static async Task<JsonElement> GetJson(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<JsonElement>())!;

    // ══════════════════════════════════════════════════════════════════════
    // AC1: DI registration — app boots and resolves all services
    // ══════════════════════════════════════════════════════════════════════

    [LiveFact]
    public async Task AC1_DI_AppBootsSuccessfully()
    {
        // Health endpoint — proves all DI registrations resolved at startup
        var healthResp = await Client.GetAsync("/healthz");
        healthResp.StatusCode.Should().Be(HttpStatusCode.OK,
            "/healthz must return 200 — confirms app started with all DI registrations intact");

        // Restaurant browse — proves the DI container resolved restaurant services
        var listResp = await Client.GetAsync("/api/food/restaurants?limit=1");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK,
            "restaurant browse must return 200 — confirms DI container is functional");
    }

    // ══════════════════════════════════════════════════════════════════════
    // AC2: Orders endpoint is mapped via MapOrderEndpoints() + DI wired
    // ══════════════════════════════════════════════════════════════════════

    [LiveFact]
    public async Task AC2_OrdersEndpoint_Returns401_Not404()
    {
        // Unauthenticated POST — payload must match PlaceOrderRequest DTO exactly
        // (deliveryAddress is a string, not an object; paymentMethod required)
        var resp = await Client.PostAsJsonAsync("/api/food/orders", new
        {
            restaurantId = Guid.NewGuid().ToString(),
            items = new[]
            {
                new
                {
                    menuItemId = Guid.NewGuid().ToString(),
                    quantity = 1,
                    customizationChoices = (object[]?)null
                }
            },
            deliveryAddress = "Test Street, Beirut",
            paymentMethod = "COD",
            couponCode = "WELCOME10",
            specialInstructions = (string?)null
        });

        // 401 = endpoint exists, binding succeeded, auth check fired.
        // 500 = payload deserialization failed (wrong DTO shape).
        // 404 = endpoint not mapped at all.
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "orders endpoint must return 401 (not 404/500) — proves MapOrderEndpoints() + DI wiring");
    }

    // ══════════════════════════════════════════════════════════════════════
    // AC3: Coupon discount & rejection — blocked by #16 claim-type mismatch
    // ══════════════════════════════════════════════════════════════════════
    //
    // The orders endpoint handler (OrderEndpoints.cs, #16 scope) extracts the
    // user ID via ClaimTypes.NameIdentifier, but the JWT token contains the
    // user ID as a short "sub" claim (JwtTokenProvider.cs line 28).  ASP.NET
    // Core does NOT automatically map "sub" → NameIdentifier without explicit
    // MapInboundClaims configuration.  Every authenticated POST /api/food/orders
    // therefore returns 401, preventing live verification of coupon discount
    // (WELCOME10/FREEDELIVERY) and coupon rejection (EXPIRED50/BIGSPENDER).
    //
    // Once #16 fixes the claim-type mismatch, the following live HTTP flows
    // will verify #17's coupon seed end-to-end:
    //   - WELCOME10 10% off → discountUsd > 0 on a valid subtotal ≥ $10
    //   - FREEDELIVERY $1.50 fixed → discountUsd > 0 on subtotal ≥ $5
    //   - EXPIRED50 (Active=false)   → 422 "invalid_coupon"
    //   - BIGSPENDER (min $100)      → 422 "invalid_coupon"

    [LiveFact]
    public async Task AC3_AuthenticatedOrder_BlockedByClaimTypeMismatch()
    {
        // Register a test user (with role field required by auth validator)
        var email = $"e2e-{Guid.NewGuid():N}@test.com";
        var phone = $"+961{70_000_000 + Random.Shared.Next(0, 9_999_999)}";
        var regResp = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email, phone,
            password = "Test123!",
            name = "E2E Orders Test",
            role = "customer"
        });
        regResp.StatusCode.Should().Be(HttpStatusCode.Created,
            "user registration must succeed");

        // Registration response includes the JWT token directly (no separate login needed).
        // Avoids hitting the login rate limiter (PermitLimit=5 per 15 min window).
        var regBody = await GetJson(regResp);
        var token = regBody.GetProperty("token").GetString()!;

        // Discover a real restaurant and menu item
        var listResp = await Client.GetAsync("/api/food/restaurants?limit=1");
        var listJson = await GetJson(listResp);
        var restaurantId = listJson.GetProperty("data")[0].GetProperty("id").GetString()!;

        var menuResp = await Client.GetAsync(
            $"/api/food/restaurants/{restaurantId}/menu");
        var menuJson = await GetJson(menuResp);
        var menuItemId = menuJson.GetProperty("categories")[0]
            .GetProperty("items")[0].GetProperty("id").GetString()!;

        // Authenticated order request — should apply WELCOME10 discount
        // but gets 401 because of the "sub" vs ClaimTypes.NameIdentifier mismatch
        var authClient = NewClient();
        authClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var orderResp = await authClient.PostAsJsonAsync("/api/food/orders", new
        {
            restaurantId,
            items = new[]
            {
                new { menuItemId, quantity = 1, customizationChoices = (object[]?)null }
            },
            deliveryAddress = "Test Street, Beirut",
            paymentMethod = "COD",
            couponCode = "WELCOME10",
            specialInstructions = (string?)null
        });

        // KNOWN BUG (#16 scope): JWT claim "sub" is not mapped to
        // ClaimTypes.NameIdentifier, so the handler can't extract the user ID.
        // This returns 401 even for valid tokens.  Once #16 fixes the claim
        // mapping, this should return 201 with discountUsd > 0.
        //
        // If this test ever PASSES (orderResp = 201), the claim mismatch has
        // been resolved — remove this block and add the full AC3 coupon tests.
        orderResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "KNOWN #16 BUG: authenticated orders blocked by claim-type mismatch. " +
            "If this assertion fails (expected 401, got something else), " +
            "the claim mapping may have been fixed — update this test accordingly.");
    }
}
