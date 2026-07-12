namespace Tekram.Tests.Orders;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tekram.Api.src.shared;
using Tekram.Tests.Fixtures;

[Trait("issue", "16")]
public class OrderIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;
    private Guid _restaurantId;
    private Guid _menuItemId;

    public OrderIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        var (client, _, _) = await AuthHelper.RegisterVerifiedUserAsync(_factory);
        _client = client;

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TekramDbContext>();
        var r = db.Restaurants.First(r => r.Status == "active" && r.DeletedAt == null);
        _restaurantId = r.Id;
        // Deterministic pick: highest-priced eligible item (tiebreak by Id) so qty=2 reliably
        // clears WELCOME10's $10 MinSubtotalUsd across every seeded restaurant (see DbInitializer.cs) —
        // an unordered .First() previously let Postgres return any matching row, sometimes a
        // cheap item that made PlaceOrder_ValidCoupon_AppliesDiscount flaky/red.
        var m = db.MenuItems
            .Where(m => m.RestaurantId == _restaurantId && m.DeletedAt == null && (m.StockCount == null || m.StockCount > 0))
            .OrderByDescending(m => m.PriceUsd)
            .ThenBy(m => m.Id)
            .First();
        _menuItemId = m.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private object BuildRequest(Guid? itemId = null, int qty = 1, string? coupon = null) => new
    {
        restaurantId = _restaurantId,
        items = new[] { new { menuItemId = itemId ?? _menuItemId, quantity = qty, customizationChoices = Array.Empty<object>() } },
        deliveryAddress = "123 Test St",
        paymentMethod = "COD",
        couponCode = coupon
    };

    [Fact]
    public async Task PlaceOrder_ValidRequest_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/food/orders", BuildRequest());
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("bookingId").ValueKind.Should().Be(JsonValueKind.String);
        body.GetProperty("status").GetString().Should().Be("pending");
        body.GetProperty("createdAt").ValueKind.Should().Be(JsonValueKind.String);
    }

    [Fact]
    public async Task PlaceOrder_NoAuth_Returns401()
    {
        var anon = _factory.CreateClient();
        var response = await anon.PostAsJsonAsync("/api/food/orders", BuildRequest());
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PlaceOrder_UnverifiedUser_Returns403()
    {
        // Register without verifying
        var regClient = _factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var reg = await regClient.PostAsJsonAsync("/api/auth/register", new
        {
            name = "UV", email = $"uv{suffix}@t.com", phone = $"+96170{Random.Shared.Next(10000, 99999)}",
            password = "Password1", role = "customer"
        });
        reg.EnsureSuccessStatusCode();
        var token = (await reg.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();

        var uvClient = _factory.CreateClient();
        uvClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await uvClient.PostAsJsonAsync("/api/food/orders", BuildRequest());
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString().Should().Be("verification_required");
    }

    [Fact]
    public async Task PlaceOrder_InvalidCoupon_Returns422()
    {
        var response = await _client.PostAsJsonAsync("/api/food/orders", BuildRequest(coupon: "NONEXISTENT"));
        response.StatusCode.Should().Be((HttpStatusCode)422);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString().Should().Be("invalid_coupon");
    }

    [Fact]
    public async Task PlaceOrder_ValidCoupon_AppliesDiscount()
    {
        var response = await _client.PostAsJsonAsync("/api/food/orders", BuildRequest(qty: 2, coupon: "WELCOME10"));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var totals = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("totals");
        totals.GetProperty("discountUsd").GetDecimal().Should().BeGreaterThan(0);
    }
}
