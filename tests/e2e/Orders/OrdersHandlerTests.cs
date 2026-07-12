namespace Tekram.E2E.Orders;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

/// <summary>
/// Black-box HTTP e2e tests for POST /api/food/orders (issue #16).
///
/// Verifies against the live API at E2E_BASE_URL, per the shared LiveFact/LiveApiTestBase
/// convention introduced by issue #60 (replaces the earlier in-process
/// WebApplicationFactory&lt;Program&gt; approach, which duplicated its own host/DB
/// bring-up and broke once the lane's DATABASE_URL moved to the postgres:// URI form —
/// Npgsql's connection-string parser doesn't accept that form directly).
///
/// The one piece of direct DB access here (seeding known OTP codes so registration can
/// be verified without a real mail/SMS provider) uses the same raw-Npgsql,
/// no-ProjectReference-to-src carve-out established in Shared/SharedKernelTests.cs —
/// the URI is parsed by hand into an NpgsqlConnectionStringBuilder.
///
/// Verifies all acceptance criteria: 201, 401, 403, 422, 409.
/// </summary>
[Trait("issue", "16")]
public class OrdersHandlerTests : LiveApiTestBase, IAsyncLifetime
{
    private static readonly string DatabaseUrl =
        Environment.GetEnvironmentVariable("E2E_DATABASE_URL")
        ?? Environment.GetEnvironmentVariable("DATABASE_URL")
        ?? "postgres://postgres:postgres@localhost:5432/tekram_lane2";

    private string _token = null!;
    private Guid _restaurantId;
    private Guid _menuItemId;

    public async Task InitializeAsync()
    {
        _token = await RegisterVerifiedUserAsync();

        // Discover a real restaurant + its highest-priced available item via HTTP.
        // Deterministic pick (highest price, tiebreak by Id) so qty=2 reliably clears
        // WELCOME10's $10 MinSubtotalUsd across every seeded restaurant — an unordered
        // pick previously let a cheap item make AC5 flaky/red (issue #16).
        var listResp = await Client.GetAsync("/api/food/restaurants?limit=50");
        var listJson = await listResp.Content.ReadFromJsonAsync<JsonElement>();
        _restaurantId = listJson.GetProperty("data")[0].GetProperty("id").GetGuid();

        var menuResp = await Client.GetAsync($"/api/food/restaurants/{_restaurantId}/menu");
        var menuJson = await menuResp.Content.ReadFromJsonAsync<JsonElement>();

        _menuItemId = menuJson.GetProperty("categories").EnumerateArray()
            .SelectMany(c => c.GetProperty("items").EnumerateArray())
            .Where(i => i.GetProperty("isAvailable").GetBoolean())
            .Select(i => (Id: i.GetProperty("id").GetGuid(), Price: i.GetProperty("priceUsd").GetDecimal()))
            .OrderByDescending(i => i.Price)
            .ThenBy(i => i.Id)
            .First().Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── Helpers ───────────────────────────────────────────────────────

    private async Task<string> RegisterVerifiedUserAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"e2e{suffix}@test.com";
        var phone = $"+96170{Random.Shared.Next(10000, 99999)}";

        var reg = await Client.PostAsJsonAsync("/api/auth/register", new
        { name = "E2E User", email, phone, password = "Password1", role = "customer" });
        reg.EnsureSuccessStatusCode();
        var auth = await reg.Content.ReadFromJsonAsync<JsonElement>();
        var userId = auth.GetProperty("id").GetGuid();
        var regToken = auth.GetProperty("token").GetString();

        // Seed helper: insert known OTP codes directly (raw Npgsql, no EF, no
        // ProjectReference to src/**) so verification can complete black-box.
        InsertKnownOtpCodes(userId);

        using var verifyClient = NewClient();
        verifyClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", regToken);
        await verifyClient.PostAsJsonAsync("/api/auth/verify/email", new { code = "123456" });
        await verifyClient.PostAsJsonAsync("/api/auth/verify/phone", new { code = "123456" });

        var login = await Client.PostAsJsonAsync("/api/auth/login",
            new { identifier = email, password = "Password1" });
        login.EnsureSuccessStatusCode();
        var loginAuth = await login.Content.ReadFromJsonAsync<JsonElement>();
        return loginAuth.GetProperty("token").GetString()!;
    }

    private static void InsertKnownOtpCodes(Guid userId)
    {
        var uri = new Uri(DatabaseUrl);
        var userInfo = uri.UserInfo.Split(':');
        var csb = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = userInfo[0],
            Password = userInfo.Length > 1 ? userInfo[1] : string.Empty,
        };

        using var conn = new Npgsql.NpgsqlConnection(csb.ConnectionString);
        conn.Open();

        var hash = BCrypt.Net.BCrypt.HashPassword("123456", 12);

        foreach (var channel in new[] { "email", "phone" })
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "INSERT INTO auth.otp_codes (id, user_id, channel, code_hash, expires_at, created_at) " +
                "VALUES (@id, @user_id, @channel, @code_hash, @expires_at, @created_at)";
            cmd.Parameters.AddWithValue("id", Guid.NewGuid());
            cmd.Parameters.AddWithValue("user_id", userId);
            cmd.Parameters.AddWithValue("channel", channel);
            cmd.Parameters.AddWithValue("code_hash", hash);
            cmd.Parameters.AddWithValue("expires_at", DateTime.UtcNow.AddMinutes(10));
            cmd.Parameters.AddWithValue("created_at", DateTime.UtcNow);
            cmd.ExecuteNonQuery();
        }
    }

    private object OrderBody(Guid? itemId = null, int qty = 1, string? coupon = null) => new
    {
        restaurantId = _restaurantId,
        items = new[] { new { menuItemId = itemId ?? _menuItemId, quantity = qty, customizationChoices = Array.Empty<object>() } },
        deliveryAddress = "123 Test St",
        paymentMethod = "COD",
        couponCode = coupon
    };

    private HttpClient AuthedClient()
    {
        var client = NewClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        return client;
    }

    // ── AC: 201 on valid order ──────────────────────────────────────────

    [LiveFact]
    public async Task AC1_PlaceOrder_ValidRequest_Returns201()
    {
        using var client = AuthedClient();
        var response = await client.PostAsJsonAsync("/api/food/orders", OrderBody());
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("bookingId").GetGuid().Should().NotBeEmpty();
        body.GetProperty("status").GetString().Should().Be("pending");
        body.GetProperty("totals").GetProperty("subtotalUsd").GetDecimal().Should().BeGreaterThan(0);
        body.GetProperty("createdAt").GetDateTime().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    // ── AC: 401 without auth ────────────────────────────────────────────

    [LiveFact]
    public async Task AC2_PlaceOrder_NoAuth_Returns401()
    {
        using var anon = NewClient();
        var response = await anon.PostAsJsonAsync("/api/food/orders", OrderBody());
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── AC: 403 when user not verified ──────────────────────────────────

    [LiveFact]
    public async Task AC3_PlaceOrder_UnverifiedUser_Returns403()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var reg = await Client.PostAsJsonAsync("/api/auth/register", new
        { name = "UV", email = $"uv{suffix}@t.com", phone = $"+96170{Random.Shared.Next(10000, 99999)}", password = "Password1", role = "customer" });
        reg.EnsureSuccessStatusCode();
        var token = (await reg.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();

        using var uvClient = NewClient();
        uvClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await uvClient.PostAsJsonAsync("/api/food/orders", OrderBody());
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString().Should().Be("verification_required");
    }

    // ── AC: 422 when coupon invalid ─────────────────────────────────────

    [LiveFact]
    public async Task AC4_PlaceOrder_InvalidCoupon_Returns422()
    {
        using var client = AuthedClient();
        var response = await client.PostAsJsonAsync("/api/food/orders", OrderBody(coupon: "NONEXISTENT"));
        response.StatusCode.Should().Be((HttpStatusCode)422);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString().Should().Be("invalid_coupon");
    }

    // ── AC: Valid coupon applies discount ────────────────────────────────

    [LiveFact]
    public async Task AC5_PlaceOrder_ValidCoupon_AppliesDiscount()
    {
        using var client = AuthedClient();
        var response = await client.PostAsJsonAsync("/api/food/orders", OrderBody(qty: 2, coupon: "WELCOME10"));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var totals = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("totals");
        totals.GetProperty("discountUsd").GetDecimal().Should().BeGreaterThan(0);
    }

    // ── AC: 409 when item unavailable ────────────────────────────────────

    [LiveFact]
    public async Task AC6_PlaceOrder_NonExistentItem_Returns409()
    {
        using var client = AuthedClient();
        var response = await client.PostAsJsonAsync("/api/food/orders", OrderBody(itemId: Guid.NewGuid()));
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString().Should().Be("item_unavailable");
    }
}
