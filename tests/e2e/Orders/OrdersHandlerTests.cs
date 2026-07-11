namespace Tekram.E2E.Orders;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Tekram.Api.src.auth.Application.Interfaces;
using Tekram.Api.src.auth.Domain;
using Tekram.Api.src.shared;

/// <summary>
/// Black-box HTTP e2e tests for POST /api/food/orders (issue #16).
/// Uses WebApplicationFactory against the lane PostgreSQL database.
/// Verifies all acceptance criteria: 201, 401, 403, 422.
/// </summary>
[Trait("issue", "16")]
public class OrdersHandlerTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private HttpClient _client = null!;
    private Guid _restaurantId;
    private Guid _menuItemId;

    public OrdersHandlerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection",
                Environment.GetEnvironmentVariable("E2E_DATABASE_URL")
                ?? Environment.GetEnvironmentVariable("DATABASE_URL")
                ?? "Host=localhost;Port=5432;Database=tekram;Username=postgres;Password=postgres");
            builder.UseSetting("EMAIL_MOCK", "true");
            builder.UseSetting("SMS_MOCK", "true");
        });
    }

    public async Task InitializeAsync()
    {
        (_client, _, _) = await RegisterVerifiedUserAsync();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TekramDbContext>();
        _restaurantId = db.Restaurants.First(r => r.Status == "active" && r.DeletedAt == null).Id;
        _menuItemId = db.MenuItems.First(m => m.RestaurantId == _restaurantId && m.DeletedAt == null
            && (m.StockCount == null || m.StockCount > 0)).Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── Helpers ───────────────────────────────────────────────────────

    private async Task<(HttpClient Client, string Token, Guid UserId)> RegisterVerifiedUserAsync()
    {
        var client = _factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"e2e{suffix}@test.com";
        var phone = $"+96170{Random.Shared.Next(10000, 99999)}";

        // Register
        var reg = await client.PostAsJsonAsync("/api/auth/register", new
        { name = "E2E User", email, phone, password = "Password1", role = "customer" });
        reg.EnsureSuccessStatusCode();
        var auth = await reg.Content.ReadFromJsonAsync<JsonElement>();
        var token = auth.GetProperty("token").GetString()!;
        var userId = auth.GetProperty("id").GetGuid();

        // Insert known OTP codes directly into DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TekramDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var hash = hasher.Hash("123456");
        db.OtpCodes.Add(new OtpCode { Id = Guid.NewGuid(), UserId = userId, Channel = "email", CodeHash = hash, ExpiresAt = DateTime.UtcNow.AddMinutes(10), CreatedAt = DateTime.UtcNow });
        db.OtpCodes.Add(new OtpCode { Id = Guid.NewGuid(), UserId = userId, Channel = "phone", CodeHash = hash, ExpiresAt = DateTime.UtcNow.AddMinutes(10), CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        // Verify both channels
        var regClient = _factory.CreateClient();
        regClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await regClient.PostAsJsonAsync("/api/auth/verify/email", new { code = "123456" });
        await regClient.PostAsJsonAsync("/api/auth/verify/phone", new { code = "123456" });

        // Re-login
        var login = await client.PostAsJsonAsync("/api/auth/login", new { identifier = email, password = "Password1" });
        login.EnsureSuccessStatusCode();
        var loginAuth = await login.Content.ReadFromJsonAsync<JsonElement>();
        var verifiedToken = loginAuth.GetProperty("token").GetString()!;

        var authClient = _factory.CreateClient();
        authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", verifiedToken);
        return (authClient, verifiedToken, userId);
    }

    private object OrderBody(Guid? itemId = null, int qty = 1, string? coupon = null) => new
    {
        restaurantId = _restaurantId,
        items = new[] { new { menuItemId = itemId ?? _menuItemId, quantity = qty, customizationChoices = Array.Empty<object>() } },
        deliveryAddress = "123 Test St",
        paymentMethod = "COD",
        couponCode = coupon
    };

    // ── AC: 201 on valid order ──────────────────────────────────────────

    [Fact]
    public async Task AC1_PlaceOrder_ValidRequest_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/food/orders", OrderBody());
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("bookingId").GetGuid().Should().NotBeEmpty();
        body.GetProperty("status").GetString().Should().Be("pending");
        body.GetProperty("totals").GetProperty("subtotalUsd").GetDecimal().Should().BeGreaterThan(0);
        body.GetProperty("createdAt").GetDateTime().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    // ── AC: 401 without auth ────────────────────────────────────────────

    [Fact]
    public async Task AC2_PlaceOrder_NoAuth_Returns401()
    {
        var anon = _factory.CreateClient();
        var response = await anon.PostAsJsonAsync("/api/food/orders", OrderBody());
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── AC: 403 when user not verified ──────────────────────────────────

    [Fact]
    public async Task AC3_PlaceOrder_UnverifiedUser_Returns403()
    {
        var client = _factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var reg = await client.PostAsJsonAsync("/api/auth/register", new
        { name = "UV", email = $"uv{suffix}@t.com", phone = $"+96170{Random.Shared.Next(10000, 99999)}", password = "Password1", role = "customer" });
        reg.EnsureSuccessStatusCode();
        var token = (await reg.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();

        var uvClient = _factory.CreateClient();
        uvClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await uvClient.PostAsJsonAsync("/api/food/orders", OrderBody());
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString().Should().Be("verification_required");
    }

    // ── AC: 422 when coupon invalid ─────────────────────────────────────

    [Fact]
    public async Task AC4_PlaceOrder_InvalidCoupon_Returns422()
    {
        var response = await _client.PostAsJsonAsync("/api/food/orders", OrderBody(coupon: "NONEXISTENT"));
        response.StatusCode.Should().Be((HttpStatusCode)422);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString().Should().Be("invalid_coupon");
    }

    // ── AC: Valid coupon applies discount ────────────────────────────────

    [Fact]
    public async Task AC5_PlaceOrder_ValidCoupon_AppliesDiscount()
    {
        var response = await _client.PostAsJsonAsync("/api/food/orders", OrderBody(qty: 2, coupon: "WELCOME10"));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var totals = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("totals");
        totals.GetProperty("discountUsd").GetDecimal().Should().BeGreaterThan(0);
    }

    // ── AC: 409 when item unavailable ────────────────────────────────────

    [Fact]
    public async Task AC6_PlaceOrder_NonExistentItem_Returns409()
    {
        var response = await _client.PostAsJsonAsync("/api/food/orders", OrderBody(itemId: Guid.NewGuid()));
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString().Should().Be("item_unavailable");
    }
}
