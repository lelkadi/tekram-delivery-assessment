namespace Tekram.E2E.Orders;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

/// <summary>
/// Live HTTP e2e tests for issue #17 (Part 2 Slice 3.3 — Coupon seed data,
/// orders DI registration, endpoint mapping).
///
/// Every AC exercises the running app via HTTP against E2E_BASE_URL:
/// registers a test user, authenticates, then places orders with each
/// coupon code to verify seed data, DI wiring, and discount/rejection
/// behaviour end-to-end.
/// </summary>
[Trait("issue", "17")]
public class OrdersCouponSeedTests
{
    private static readonly string? BaseUrl = Environment.GetEnvironmentVariable("E2E_BASE_URL");

    private static HttpClient NewClient() =>
        new() { BaseAddress = new Uri((BaseUrl ?? "http://localhost:3021").TrimEnd('/')) };

    // ── helpers ──────────────────────────────────────────────────────────

    private static async Task<string> RegisterAndGetTokenAsync(HttpClient client)
    {
        var email = $"e2e-{Guid.NewGuid():N}@test.com";
        var phone = $"+961{70_000_000 + Random.Shared.Next(0, 9_999_999)}";

        // Register
        var regResp = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            phone,
            password = "Test123!",
            name = "E2E Test User"
        });
        regResp.StatusCode.Should().Be(HttpStatusCode.Created,
            "user registration must succeed before placing orders");

        // Login
        var loginResp = await client.PostAsJsonAsync("/api/auth/login", new
        {
            identifier = email,
            password = "Test123!"
        });
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK,
            "login must succeed after registration");

        var loginBody = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginBody.GetProperty("token").GetString();
        token.Should().NotBeNullOrWhiteSpace("login response must include a JWT token");
        return token!;
    }

    private static HttpClient AuthenticatedClient(string token)
    {
        var client = NewClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<JsonElement> PostOrderAsync(
        HttpClient authClient, string couponCode)
    {
        var payload = new
        {
            restaurantId = "",   // filled below
            items = new[]
            {
                new
                {
                    menuItemId = "",
                    quantity = 1,
                    customizationChoices = (string[]?)null
                }
            },
            couponCode,
            deliveryAddress = new
            {
                street = "Test Street 1",
                city = "Beirut",
                building = "A",
                floor = "2",
                notes = (string?)null
            }
        };

        // Discover a restaurant and menu item
        var listResp = await authClient.GetAsync("/api/food/restaurants?limit=1");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var listJson = await listResp.Content.ReadFromJsonAsync<JsonElement>();
        var restaurants = listJson.GetProperty("data").EnumerateArray().ToList();
        restaurants.Should().NotBeEmpty("at least one restaurant must be seeded");

        var restaurant = restaurants.First();
        var restaurantId = restaurant.GetProperty("id").GetString()!;
        var restaurantName = restaurant.GetProperty("name").GetString()!;

        // Get menu
        var menuResp = await authClient.GetAsync($"/api/food/restaurants/{restaurantId}/menu");
        menuResp.StatusCode.Should().Be(HttpStatusCode.OK,
            $"menu for '{restaurantName}' must be available");
        var menuJson = await menuResp.Content.ReadFromJsonAsync<JsonElement>();
        var firstItem = menuJson.GetProperty("categories")
            .EnumerateArray()
            .SelectMany(c => c.GetProperty("items").EnumerateArray())
            .FirstOrDefault();
        firstItem.ValueKind.Should().Be(JsonValueKind.Object,
            "menu must contain at least one item");

        var menuItemId = firstItem.GetProperty("id").GetString()!;

        // Place order
        var orderPayload = new
        {
            restaurantId,
            items = new[]
            {
                new
                {
                    menuItemId,
                    quantity = 1,
                    customizationChoices = (string[]?)null
                }
            },
            couponCode,
            deliveryAddress = new
            {
                street = "Test Street 1",
                city = "Beirut",
                building = "A",
                floor = "2",
                notes = (string?)null
            }
        };

        var resp = await authClient.PostAsJsonAsync("/api/food/orders", orderPayload);
        return await resp.Content.ReadFromJsonAsync<JsonElement>();
    }

    // ── AC1: Valid coupon applies discount ───────────────────────────────

    [SkippableFact]
    public async Task AC1_ValidCoupon_AppliesDiscount()
    {
        Skip.If(string.IsNullOrWhiteSpace(BaseUrl),
            "E2E_BASE_URL not set — no live lane API to test against");

        using var client = NewClient();
        var token = await RegisterAndGetTokenAsync(client);
        using var authClient = AuthenticatedClient(token);

        // WELCOME10: 10% off, min $10
        var order = await PostOrderAsync(authClient, "WELCOME10");
        order.GetProperty("bookingId").ValueKind.Should().Be(JsonValueKind.String);
        order.GetProperty("discountUsd").ValueKind.Should().Be(JsonValueKind.Number);
        order.GetProperty("discountUsd").GetDecimal().Should().BeGreaterThan(0,
            "WELCOME10 must apply a non-zero discount on a valid order");
    }

    [SkippableFact]
    public async Task AC1_FixedCoupon_AppliesDiscount()
    {
        Skip.If(string.IsNullOrWhiteSpace(BaseUrl),
            "E2E_BASE_URL not set — no live lane API to test against");

        using var client = NewClient();
        var token = await RegisterAndGetTokenAsync(client);
        using var authClient = AuthenticatedClient(token);

        // FREEDELIVERY: fixed $1.50 off, min $5
        var order = await PostOrderAsync(authClient, "FREEDELIVERY");
        order.GetProperty("bookingId").ValueKind.Should().Be(JsonValueKind.String);
        order.GetProperty("discountUsd").ValueKind.Should().Be(JsonValueKind.Number);
        order.GetProperty("discountUsd").GetDecimal().Should().BeGreaterThan(0,
            "FREEDELIVERY must apply a non-zero discount on a valid order");
    }

    // ── AC2: Inactive / expired coupon → 422 ────────────────────────────

    [SkippableFact]
    public async Task AC2_InactiveCoupon_Returns422()
    {
        Skip.If(string.IsNullOrWhiteSpace(BaseUrl),
            "E2E_BASE_URL not set — no live lane API to test against");

        using var client = NewClient();
        var token = await RegisterAndGetTokenAsync(client);
        using var authClient = AuthenticatedClient(token);

        // EXPIRED50: Active=false
        var resp = await authClient.PostAsJsonAsync("/api/food/orders", new
        {
            restaurantId = "00000000-0000-0000-0000-000000000000",
            items = new[]
            {
                new
                {
                    menuItemId = "00000000-0000-0000-0000-000000000000",
                    quantity = 1,
                    customizationChoices = (string[]?)null
                }
            },
            couponCode = "EXPIRED50",
            deliveryAddress = new
            {
                street = "x", city = "x", building = "x", floor = "1",
                notes = (string?)null
            }
        });

        // 422 from the coupon validation (not from restaurant/item lookup)
        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("error").GetString().Should().Contain("coupon",
            "rejection must reference the coupon");
    }

    // ── AC3: Min-subtotal-not-met coupon → 422 ──────────────────────────

    [SkippableFact]
    public async Task AC3_MinSubtotalNotMet_Returns422()
    {
        Skip.If(string.IsNullOrWhiteSpace(BaseUrl),
            "E2E_BASE_URL not set — no live lane API to test against");

        using var client = NewClient();
        var token = await RegisterAndGetTokenAsync(client);
        using var authClient = AuthenticatedClient(token);

        // BIGSPENDER: 20% off, min $100. A single item will be well below.
        // Use bogus restaurant/item IDs — coupon validation (min subtotal)
        // fires before restaurant lookup in the handler chain.
        var resp = await authClient.PostAsJsonAsync("/api/food/orders", new
        {
            restaurantId = "00000000-0000-0000-0000-000000000000",
            items = new[]
            {
                new
                {
                    menuItemId = "00000000-0000-0000-0000-000000000000",
                    quantity = 1,
                    customizationChoices = (string[]?)null
                }
            },
            couponCode = "BIGSPENDER",
            deliveryAddress = new
            {
                street = "x", city = "x", building = "x", floor = "1",
                notes = (string[]?)null
            }
        });

        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("error").GetString().Should().Contain("coupon",
            "rejection must reference the coupon");
    }

    // ── AC4: Orders endpoint is mapped (DI + Program.cs wiring) ─────────

    [SkippableFact]
    public async Task AC4_OrdersEndpoint_Returns401_Not404()
    {
        Skip.If(string.IsNullOrWhiteSpace(BaseUrl),
            "E2E_BASE_URL not set — no live lane API to test against");

        using var client = NewClient();

        // Unauthenticated POST to /api/food/orders
        var resp = await client.PostAsJsonAsync("/api/food/orders", new
        {
            restaurantId = Guid.NewGuid().ToString(),
            items = new[]
            {
                new
                {
                    menuItemId = Guid.NewGuid().ToString(),
                    quantity = 1,
                    customizationChoices = (string[]?)null
                }
            },
            couponCode = "WELCOME10",
            deliveryAddress = new
            {
                street = "Test", city = "Test", building = "1", floor = "1",
                notes = (string?)null
            }
        });

        // 401 = endpoint exists and requires auth; 404 = not mapped at all
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "orders endpoint must be mapped (401) not missing (404) — verifies MapOrderEndpoints() + DI wiring");
    }

    // ── AC5: DI registration — app boots and restaurant endpoints work ──

    [SkippableFact]
    public async Task AC5_AppBootsAndRestaurantEndpointsWork()
    {
        Skip.If(string.IsNullOrWhiteSpace(BaseUrl),
            "E2E_BASE_URL not set — no live lane API to test against");

        using var client = NewClient();

        // Health endpoint
        var healthResp = await client.GetAsync("/healthz");
        healthResp.StatusCode.Should().Be(HttpStatusCode.OK,
            "health endpoint must return 200 — confirms app booted with all DI registrations");

        // Restaurant endpoint (proves the DI container resolved everything)
        var listResp = await client.GetAsync("/api/food/restaurants?limit=1");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK,
            "restaurant browse endpoint must return 200 — confirms DI container resolved correctly");
    }
}
