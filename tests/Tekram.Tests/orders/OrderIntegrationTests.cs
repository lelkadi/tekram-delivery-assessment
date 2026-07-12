namespace Tekram.Tests.Orders;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Tekram.Api.src.orders.Application.DTOs;
using Tekram.Api.src.orders.Domain;
using Tekram.Api.src.shared;
using Tekram.Tests.Fixtures;

public class OrderIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public OrderIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Registers a fresh user, verifies both email and phone via API,
    /// and returns the client, JWT token, and user ID.
    /// </summary>
    private async Task<(HttpClient Client, string Token, Guid UserId)> RegisterAndVerifyAsync()
    {
        var (client, token, userId) = await AuthHelper.RegisterAndGetToken(_factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Verify email
        var emailResponse = await client.PostAsJsonAsync("/api/auth/verify/email", new { code = "123456" });
        emailResponse.EnsureSuccessStatusCode();

        // Verify phone (separate OTP was inserted by AuthHelper)
        var phoneResponse = await client.PostAsJsonAsync("/api/auth/verify/phone", new { code = "123456" });
        phoneResponse.EnsureSuccessStatusCode();

        return (client, token, userId);
    }

    /// <summary>
    /// Registers a fresh user but does NOT verify any channel.
    /// </summary>
    private async Task<(HttpClient Client, string Token, Guid UserId)> RegisterOnlyAsync()
    {
        var (client, token, userId) = await AuthHelper.RegisterAndGetToken(_factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, token, userId);
    }

    /// <summary>
    /// Gets a seed menu item by name.
    /// </summary>
    private static async Task<(Guid RestaurantId, Guid MenuItemId, decimal PriceUsd)> GetItemByNameAsync(
        CustomWebApplicationFactory factory, string name)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TekramDbContext>();
        var item = await db.MenuItems.FirstAsync(m => m.Name == name);
        return (item.RestaurantId, item.Id, item.PriceUsd);
    }

    /// <summary>
    /// Gets a seed menu item with a limited (non-null) stock count.
    /// </summary>
    private static async Task<(Guid RestaurantId, Guid MenuItemId, int StockCount)> GetLimitedStockItemAsync(
        CustomWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TekramDbContext>();
        var item = await db.MenuItems
            .Where(m => m.StockCount != null && m.StockCount > 0)
            .FirstAsync();
        return (item.RestaurantId, item.Id, item.StockCount!.Value);
    }

    /// <summary>
    /// Deserializes a JSON error response.
    /// </summary>
    private static async Task<JsonElement> DeserializeErrorAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.ValueKind.Should().Be(JsonValueKind.Object);
        return json;
    }

    // ========================================================================
    // SUCCESS CASES
    // ========================================================================

    [Fact]
    public async Task SuccessfulOrder_NoCoupon_Returns201()
    {
        // Arrange
        var (client, _, _) = await RegisterAndVerifyAsync();
        var (restaurantId, menuItemId, price) = await GetItemByNameAsync(_factory, "Margherita Pizza");

        var body = new
        {
            restaurantId,
            items = new[]
            {
                new { menuItemId, quantity = 1, customizationChoices = Array.Empty<object>() }
            },
            deliveryAddress = "123 Main St, Beirut",
            paymentMethod = "COD"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/food/orders", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        order.Should().NotBeNull();
        order!.BookingId.Should().NotBeEmpty();
        order.Status.Should().Be("pending");
        order.Totals.SubtotalUsd.Should().Be(price);
        order.Totals.DeliveryFeeUsd.Should().Be(1.50m);
        order.Totals.SmallOrderSurchargeUsd.Should().Be(0m); // subtotal >= $7
        order.Totals.DiscountUsd.Should().Be(0m);
        order.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task SuccessfulOrder_WithPercentCoupon()
    {
        // Arrange
        var (client, _, _) = await RegisterAndVerifyAsync();
        var (restaurantId, menuItemId, price) = await GetItemByNameAsync(_factory, "Margherita Pizza");

        // SUMMER10 = 10% off, min_subtotal_usd = $10
        // Order 2 × Margherita Pizza ($7.50 each) = $15.00 → exceeds $10 min
        const int quantity = 2;
        var subtotal = price * quantity;

        var body = new
        {
            restaurantId,
            items = new[]
            {
                new { menuItemId, quantity, customizationChoices = Array.Empty<object>() }
            },
            deliveryAddress = "456 Oak Ave, Beirut",
            paymentMethod = "COD",
            couponCode = "SUMMER10"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/food/orders", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        order.Should().NotBeNull();
        order!.Totals.DiscountUsd.Should().BeGreaterThan(0);
        order.Totals.SubtotalUsd.Should().Be(subtotal);
        // total = subtotal + delivery + surcharge - discount
        var expectedTotal = subtotal + 1.50m + 0m - order.Totals.DiscountUsd;
        order.Totals.TotalUsd.Should().Be(expectedTotal);
    }

    [Fact]
    public async Task SuccessfulOrder_WithFixedCoupon()
    {
        // Arrange
        var (client, _, _) = await RegisterAndVerifyAsync();
        var (restaurantId, menuItemId, price) = await GetItemByNameAsync(_factory, "Margherita Pizza");

        // FREEDELIVERY = fixed $1.50 off, no minimum subtotal
        var body = new
        {
            restaurantId,
            items = new[]
            {
                new { menuItemId, quantity = 1, customizationChoices = Array.Empty<object>() }
            },
            deliveryAddress = "789 Pine Rd, Beirut",
            paymentMethod = "COD",
            couponCode = "FREEDELIVERY"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/food/orders", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        order.Should().NotBeNull();
        order!.Totals.DiscountUsd.Should().Be(1.50m);
        order.Totals.DeliveryFeeUsd.Should().Be(1.50m);
    }

    [Fact]
    public async Task SmallOrderSurcharge_Applied()
    {
        // Arrange
        var (client, _, _) = await RegisterAndVerifyAsync();
        var (restaurantId, menuItemId, price) = await GetItemByNameAsync(_factory, "Bruschetta");

        // Bruschetta = $5.50 → subtotal = $5.50 < $7.00 MOV → surcharge $1.00
        var body = new
        {
            restaurantId,
            items = new[]
            {
                new { menuItemId, quantity = 1, customizationChoices = Array.Empty<object>() }
            },
            deliveryAddress = "321 Elm St, Beirut",
            paymentMethod = "COD"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/food/orders", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        order.Should().NotBeNull();
        order!.Totals.SmallOrderSurchargeUsd.Should().Be(1.00m);
        order.Totals.SubtotalUsd.Should().Be(price);
        // Total = subtotal + delivery + surcharge - discount
        var expectedTotal = price + 1.50m + 1.00m - 0m;
        order.Totals.TotalUsd.Should().Be(expectedTotal);
    }

    [Fact]
    public async Task NoSurcharge_AboveMOV()
    {
        // Arrange
        var (client, _, _) = await RegisterAndVerifyAsync();
        var (restaurantId, menuItemId, price) = await GetItemByNameAsync(_factory, "Margherita Pizza");

        // Margherita Pizza = $7.50 → subtotal = $7.50 >= $7.00 MOV → no surcharge
        var body = new
        {
            restaurantId,
            items = new[]
            {
                new { menuItemId, quantity = 1, customizationChoices = Array.Empty<object>() }
            },
            deliveryAddress = "654 Cedar Ln, Beirut",
            paymentMethod = "COD"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/food/orders", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        order.Should().NotBeNull();
        order!.Totals.SmallOrderSurchargeUsd.Should().Be(0m);
        order.Totals.SubtotalUsd.Should().Be(price);
    }

    [Fact]
    public async Task DeliveryFee_AlwaysConstant()
    {
        // Arrange — use a cheap item (Bruschetta $5.50, which gets surcharge)
        var (client, _, _) = await RegisterAndVerifyAsync();
        var (restaurantId, menuItemId, _) = await GetItemByNameAsync(_factory, "Bruschetta");

        var body = new
        {
            restaurantId,
            items = new[]
            {
                new { menuItemId, quantity = 1, customizationChoices = Array.Empty<object>() }
            },
            deliveryAddress = "987 Walnut St, Beirut",
            paymentMethod = "COD"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/food/orders", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        order.Should().NotBeNull();
        order!.Totals.DeliveryFeeUsd.Should().Be(1.50m);
    }

    // ========================================================================
    // VERIFICATION GATE (403)
    // ========================================================================

    [Fact]
    public async Task UnverifiedUser_EmailOnly_Returns403()
    {
        // Arrange — register and verify only email
        var (client, _, _) = await RegisterOnlyAsync();

        // Verify email only
        var emailResponse = await client.PostAsJsonAsync("/api/auth/verify/email", new { code = "123456" });
        emailResponse.EnsureSuccessStatusCode();

        var (restaurantId, menuItemId, _) = await GetItemByNameAsync(_factory, "Margherita Pizza");

        var body = new
        {
            restaurantId,
            items = new[]
            {
                new { menuItemId, quantity = 1, customizationChoices = Array.Empty<object>() }
            },
            deliveryAddress = "111 Birch St, Beirut",
            paymentMethod = "COD"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/food/orders", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var json = await DeserializeErrorAsync(response);
        json.GetProperty("error").GetString().Should().Be("verification_required");
    }

    [Fact]
    public async Task UnverifiedUser_PhoneOnly_Returns403()
    {
        // Arrange — register and verify only phone
        var (client, _, _) = await RegisterOnlyAsync();

        // Verify phone only
        var phoneResponse = await client.PostAsJsonAsync("/api/auth/verify/phone", new { code = "123456" });
        phoneResponse.EnsureSuccessStatusCode();

        var (restaurantId, menuItemId, _) = await GetItemByNameAsync(_factory, "Margherita Pizza");

        var body = new
        {
            restaurantId,
            items = new[]
            {
                new { menuItemId, quantity = 1, customizationChoices = Array.Empty<object>() }
            },
            deliveryAddress = "222 Maple Dr, Beirut",
            paymentMethod = "COD"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/food/orders", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var json = await DeserializeErrorAsync(response);
        json.GetProperty("error").GetString().Should().Be("verification_required");
    }

    [Fact]
    public async Task UnverifiedUser_Neither_Returns403()
    {
        // Arrange — register only, no verification
        var (client, _, _) = await RegisterOnlyAsync();

        var (restaurantId, menuItemId, _) = await GetItemByNameAsync(_factory, "Margherita Pizza");

        var body = new
        {
            restaurantId,
            items = new[]
            {
                new { menuItemId, quantity = 1, customizationChoices = Array.Empty<object>() }
            },
            deliveryAddress = "333 Ash Ct, Beirut",
            paymentMethod = "COD"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/food/orders", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var json = await DeserializeErrorAsync(response);
        json.GetProperty("error").GetString().Should().Be("verification_required");
    }

    // ========================================================================
    // STOCK (409)
    // ========================================================================

    [Fact]
    public async Task ItemOutOfStock_Returns409()
    {
        // Arrange
        var (client, _, _) = await RegisterAndVerifyAsync();
        var (restaurantId, menuItemId, stockCount) = await GetLimitedStockItemAsync(_factory);

        // Request far more than available stock
        var body = new
        {
            restaurantId,
            items = new[]
            {
                new { menuItemId, quantity = 99, customizationChoices = Array.Empty<object>() }
            },
            deliveryAddress = "444 Spruce Way, Beirut",
            paymentMethod = "COD"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/food/orders", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var json = await DeserializeErrorAsync(response);
        json.GetProperty("error").GetString().Should().Be("item_unavailable");
    }

    // ========================================================================
    // COUPON VALIDATION (422)
    // ========================================================================

    [Fact]
    public async Task InvalidCouponCode_Returns422()
    {
        // Arrange
        var (client, _, _) = await RegisterAndVerifyAsync();
        var (restaurantId, menuItemId, _) = await GetItemByNameAsync(_factory, "Margherita Pizza");

        var body = new
        {
            restaurantId,
            items = new[]
            {
                new { menuItemId, quantity = 1, customizationChoices = Array.Empty<object>() }
            },
            deliveryAddress = "555 Cherry Blvd, Beirut",
            paymentMethod = "COD",
            couponCode = "FAKECODE"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/food/orders", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var json = await DeserializeErrorAsync(response);
        json.GetProperty("error").GetString().Should().Be("invalid_coupon");
    }

    [Fact]
    public async Task ExpiredCoupon_Returns422()
    {
        // Arrange
        var (client, _, _) = await RegisterAndVerifyAsync();
        var (restaurantId, menuItemId, _) = await GetItemByNameAsync(_factory, "Margherita Pizza");

        // EXPIRED50 has Active = false
        var body = new
        {
            restaurantId,
            items = new[]
            {
                new { menuItemId, quantity = 1, customizationChoices = Array.Empty<object>() }
            },
            deliveryAddress = "666 Date Palm Rd, Beirut",
            paymentMethod = "COD",
            couponCode = "EXPIRED50"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/food/orders", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var json = await DeserializeErrorAsync(response);
        json.GetProperty("error").GetString().Should().Be("invalid_coupon");
    }

    [Fact]
    public async Task CouponMinSubtotalNotMet_Returns422()
    {
        // Arrange
        var (client, _, _) = await RegisterAndVerifyAsync();
        var (restaurantId, menuItemId, price) = await GetItemByNameAsync(_factory, "Bruschetta");

        // BIGSPENDER = 20% off, min_subtotal_usd = $100
        // Bruschetta $5.50 × 1 = $5.50 → way under $100 minimum
        var body = new
        {
            restaurantId,
            items = new[]
            {
                new { menuItemId, quantity = 1, customizationChoices = Array.Empty<object>() }
            },
            deliveryAddress = "777 Vine St, Beirut",
            paymentMethod = "COD",
            couponCode = "BIGSPENDER"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/food/orders", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var json = await DeserializeErrorAsync(response);
        json.GetProperty("error").GetString().Should().Be("invalid_coupon");
    }

    // ========================================================================
    // VALIDATION (422)
    // ========================================================================

    [Fact]
    public async Task EmptyItems_Returns422()
    {
        // Arrange
        var (client, _, _) = await RegisterAndVerifyAsync();
        var (restaurantId, _, _) = await GetItemByNameAsync(_factory, "Margherita Pizza");

        var body = new
        {
            restaurantId,
            items = Array.Empty<object>(),
            deliveryAddress = "888 Olive Ct, Beirut",
            paymentMethod = "COD"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/food/orders", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var json = await DeserializeErrorAsync(response);
        json.GetProperty("error").GetString().Should().Be("validation_failed");
    }

    [Fact]
    public async Task MissingDeliveryAddress_Returns422()
    {
        // Arrange
        var (client, _, _) = await RegisterAndVerifyAsync();
        var (restaurantId, menuItemId, _) = await GetItemByNameAsync(_factory, "Margherita Pizza");

        var body = new
        {
            restaurantId,
            items = new[]
            {
                new { menuItemId, quantity = 1, customizationChoices = Array.Empty<object>() }
            },
            deliveryAddress = "",
            paymentMethod = "COD"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/food/orders", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var json = await DeserializeErrorAsync(response);
        json.GetProperty("error").GetString().Should().Be("validation_failed");
    }

    [Fact]
    public async Task InvalidPaymentMethod_Returns422()
    {
        // Arrange
        var (client, _, _) = await RegisterAndVerifyAsync();
        var (restaurantId, menuItemId, _) = await GetItemByNameAsync(_factory, "Margherita Pizza");

        var body = new
        {
            restaurantId,
            items = new[]
            {
                new { menuItemId, quantity = 1, customizationChoices = Array.Empty<object>() }
            },
            deliveryAddress = "999 Palm Blvd, Beirut",
            paymentMethod = "BITCOIN"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/food/orders", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var json = await DeserializeErrorAsync(response);
        json.GetProperty("error").GetString().Should().Be("validation_failed");
    }

    // ========================================================================
    // AUTH (401)
    // ========================================================================

    [Fact]
    public async Task NoJwtToken_Returns401()
    {
        // Arrange — no auth header
        var client = _factory.CreateClient();
        var (restaurantId, menuItemId, _) = await GetItemByNameAsync(_factory, "Margherita Pizza");

        var body = new
        {
            restaurantId,
            items = new[]
            {
                new { menuItemId, quantity = 1, customizationChoices = Array.Empty<object>() }
            },
            deliveryAddress = "1010 Harbor St, Beirut",
            paymentMethod = "COD"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/food/orders", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ========================================================================
    // MATH VERIFICATION
    // ========================================================================

    [Fact]
    public async Task MathCheck_LineItemsPersisted()
    {
        // Arrange
        var (client, _, _) = await RegisterAndVerifyAsync();
        var (restaurantId, menuItemId, price) = await GetItemByNameAsync(_factory, "Margherita Pizza");

        const int quantity = 2;
        var subtotal = price * quantity;

        var body = new
        {
            restaurantId,
            items = new[]
            {
                new { menuItemId, quantity, customizationChoices = Array.Empty<object>() }
            },
            deliveryAddress = "1111 Ocean View, Beirut",
            paymentMethod = "COD"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/food/orders", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        order.Should().NotBeNull();

        // Assert — query persisted order items from DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TekramDbContext>();
        var savedOrder = await db.Orders
            .Include(o => o.OrderItems)
            .FirstAsync(o => o.Id == order!.BookingId);

        savedOrder.Should().NotBeNull();
        savedOrder.OrderItems.Should().HaveCount(1);

        var savedItem = savedOrder.OrderItems[0];
        savedItem.MenuItemId.Should().Be(menuItemId);
        savedItem.Quantity.Should().Be(quantity);
        savedItem.UnitPriceUsd.Should().Be(price);
        savedItem.LineTotalUsd.Should().Be(price * quantity);

        // Verify totals formula: total = subtotal + delivery + surcharge - discount
        var calculatedTotal = savedOrder.SubtotalUsd
                              + savedOrder.DeliveryFeeUsd
                              + savedOrder.SmallOrderSurchargeUsd
                              - savedOrder.DiscountUsd;
        savedOrder.TotalUsd.Should().Be(calculatedTotal);
    }

    // ========================================================================
    // COUPON USAGE
    // ========================================================================

    [Fact]
    public async Task CouponUsage_Increments()
    {
        // Arrange — query current uses count for SUMMER10
        int initialUses;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TekramDbContext>();
            var coupon = await db.Coupons.FirstAsync(c => c.Code == "SUMMER10");
            initialUses = coupon.UsesCount;
        }

        var (client, _, _) = await RegisterAndVerifyAsync();
        var (restaurantId, menuItemId, price) = await GetItemByNameAsync(_factory, "Margherita Pizza");

        // Order enough to meet $10 minimum for SUMMER10
        var quantity = (int)Math.Ceiling(10m / price) + 1;
        var body = new
        {
            restaurantId,
            items = new[]
            {
                new { menuItemId, quantity, customizationChoices = Array.Empty<object>() }
            },
            deliveryAddress = "1212 Sunset Blvd, Beirut",
            paymentMethod = "COD",
            couponCode = "SUMMER10"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/food/orders", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Assert — verify uses_count increased by 1
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TekramDbContext>();
        var updatedCoupon = await verifyDb.Coupons.FirstAsync(c => c.Code == "SUMMER10");
        updatedCoupon.UsesCount.Should().Be(initialUses + 1);
    }
}
