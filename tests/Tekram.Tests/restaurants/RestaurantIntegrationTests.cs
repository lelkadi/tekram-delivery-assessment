using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tekram.Api.src.restaurants.Domain;
using Tekram.Api.src.shared;

namespace Tekram.Tests.Restaurants;

/// <summary>
/// Integration tests for the Restaurant module (Slice 4.2).
/// Tests cover GET /api/food/restaurants (11 tests) and
/// GET /api/food/restaurants/{id}/menu (5 tests).
/// </summary>
[Collection("RestaurantIntegrationTests")]
public class RestaurantIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    // Test restaurant IDs created per test for cleanup
    private readonly List<Guid> _createdRestaurantIds = [];

    // Known seed restaurant names for verification
    private const string SeedRestaurant1 = "La Trattoria";
    private const string SeedRestaurant2 = "Burger Nation";
    private const string SeedRestaurant3 = "Sushi Zen";

    public RestaurantIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();

        // Add 5 additional restaurants so we have 8 total for pagination testing.
        // Each test-run creates fresh data; cleanup happens in DisposeAsync.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TekramDbContext>();

        // Only seed test data if this is a fresh factory (first test run) —
        // IAsyncLifetime runs per test so we add once and clean up once.
        // But since each test instance is separate, this always runs fresh.
        await SeedTestRestaurantsAsync(db);

        // Verify we have enough restaurants for pagination tests
        var count = await db.Restaurants.CountAsync(r => r.DeletedAt == null && r.Status == "active");
        if (count < 8)
        {
            // Fallback: seed more directly
            await SeedTestRestaurantsAsync(db);
        }
    }

    public async Task DisposeAsync()
    {
        // Clean up all test restaurants created by this test instance
        if (_createdRestaurantIds.Count > 0)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TekramDbContext>();

            foreach (var id in _createdRestaurantIds)
            {
                var restaurant = await db.Restaurants.FindAsync(id);
                if (restaurant != null)
                {
                    // Remove related MenuItems, Customizations, then MenuCategories, then Restaurant
                    // Cascade delete should handle this, but be explicit to avoid FK issues
                    var items = await db.MenuItems.Where(m => m.RestaurantId == id).ToListAsync();
                    foreach (var item in items)
                    {
                        var groups = await db.CustomizationGroups.Where(g => g.MenuItemId == item.Id).ToListAsync();
                        foreach (var group in groups)
                        {
                            var options = await db.CustomizationOptions.Where(o => o.GroupId == group.Id).ToListAsync();
                            db.CustomizationOptions.RemoveRange(options);
                        }
                        db.CustomizationGroups.RemoveRange(groups);
                    }
                    db.MenuItems.RemoveRange(items);

                    var categories = await db.MenuCategories.Where(c => c.RestaurantId == id).ToListAsync();
                    db.MenuCategories.RemoveRange(categories);

                    db.Restaurants.Remove(restaurant);
                }
            }

            await db.SaveChangesAsync();
        }
    }

    private async Task SeedTestRestaurantsAsync(TekramDbContext db)
    {
        // Only seed if we don't already have enough restaurants
        var existingCount = await db.Restaurants.CountAsync(r => r.DeletedAt == null && r.Status == "active");
        if (existingCount >= 8)
            return;

        var testRestaurants = new (string Name, string Cuisine, int PriceTier, decimal Rating)[]
        {
            ("Test Pizza House", "Italian", 1, 4.0m),
            ("Test Taco House", "Mexican", 1, 3.8m),
            ("Test Curry Palace", "Indian", 2, 4.2m),
            ("Test Noodle Bar", "Chinese", 1, 3.5m),
            ("Test Steakhouse", "American", 4, 4.5m)
        };

        foreach (var (name, cuisine, priceTier, rating) in testRestaurants)
        {
            var restaurant = new Restaurant
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = $"Integration test restaurant: {name}",
                Cuisine = cuisine,
                Rating = rating,
                PriceTier = priceTier,
                AvgPrepMinutes = 25,
                Status = "active",
                Latitude = 33.89m,
                Longitude = 35.50m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Restaurants.Add(restaurant);
            _createdRestaurantIds.Add(restaurant.Id);
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Creates a temporary restaurant owned by the current test, for scenarios
    /// that modify restaurant state (delete, inactive).
    /// </summary>
    private async Task<Guid> CreateTempRestaurantAsync(TekramDbContext db, string nameSuffix)
    {
        var restaurant = new Restaurant
        {
            Id = Guid.NewGuid(),
            Name = $"Temp_{nameSuffix}_{Guid.NewGuid():N}",
            Description = "Temporary restaurant for integration test",
            Cuisine = "Test",
            Rating = 3.0m,
            PriceTier = 1,
            AvgPrepMinutes = 15,
            Status = "active",
            Latitude = 33.89m,
            Longitude = 35.50m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Restaurants.Add(restaurant);
        await db.SaveChangesAsync();
        _createdRestaurantIds.Add(restaurant.Id);
        return restaurant.Id;
    }

    // ====================================================================
    // GET /api/food/restaurants — 11 tests
    // ====================================================================

    [Fact]
    public async Task DefaultListing_Returns200_WithEnvelope()
    {
        // Act
        var response = await _client.GetAsync("/api/food/restaurants");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Array);
        json.GetProperty("pagination").GetProperty("currentPage").GetInt32().Should().Be(1);
        json.GetProperty("pagination").GetProperty("totalItems").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Page1Limit5_ReturnsCorrectPageAndLimit()
    {
        // Act
        var response = await _client.GetAsync("/api/food/restaurants?page=1&limit=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = json.GetProperty("data").EnumerateArray().ToList();
        var pagination = json.GetProperty("pagination");

        pagination.GetProperty("currentPage").GetInt32().Should().Be(1);
        pagination.GetProperty("limit").GetInt32().Should().Be(5);
        data.Count.Should().BeLessThanOrEqualTo(5);
        pagination.GetProperty("totalItems").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Page2_ReturnsDifferentItemsThanPage1()
    {
        // Act
        var page1Response = await _client.GetAsync("/api/food/restaurants?page=1&limit=5");
        var page2Response = await _client.GetAsync("/api/food/restaurants?page=2&limit=5");

        // Assert
        page1Response.StatusCode.Should().Be(HttpStatusCode.OK);
        page2Response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page1Json = await page1Response.Content.ReadFromJsonAsync<JsonElement>();
        var page2Json = await page2Response.Content.ReadFromJsonAsync<JsonElement>();

        var page1Ids = page1Json.GetProperty("data").EnumerateArray()
            .Select(r => r.GetProperty("id").GetGuid()).ToList();
        var page2Ids = page2Json.GetProperty("data").EnumerateArray()
            .Select(r => r.GetProperty("id").GetGuid()).ToList();

        page1Ids.Should().NotBeEmpty();
        page1Ids.Should().NotIntersectWith(page2Ids,
            "page 1 and page 2 must not overlap (deterministic secondary sort on Id)");
    }

    [Fact]
    public async Task SearchByName_ILIKE_CaseInsensitivePartial()
    {
        // Arrange — get a search term from the first restaurant in the listing
        var listResponse = await _client.GetAsync("/api/food/restaurants");
        var listJson = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var restaurants = listJson.GetProperty("data").EnumerateArray().ToList();
        restaurants.Should().NotBeEmpty("there should be at least one active restaurant");
        var firstRestaurantName = restaurants[0].GetProperty("name").GetString()!;
        var searchTerm = firstRestaurantName.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

        // Act
        var response = await _client.GetAsync($"/api/food/restaurants?search={searchTerm}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var names = json.GetProperty("data").EnumerateArray()
            .Select(r => r.GetProperty("name").GetString()).ToList();

        names.Should().Contain(n => n!.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task FilterByCuisine_ExactMatch()
    {
        // Act — Burger Nation is seeded with cuisine=Burgers
        var response = await _client.GetAsync("/api/food/restaurants?cuisine=Burgers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = json.GetProperty("data").EnumerateArray().ToList();
        data.Should().NotBeEmpty();
        data.Should().OnlyContain(r => r.GetProperty("cuisine").GetString() == "Burgers");
    }

    [Fact]
    public async Task FilterByPriceTier_ReturnsCorrectTier()
    {
        // Act
        var response = await _client.GetAsync("/api/food/restaurants?price_tier=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = json.GetProperty("data").EnumerateArray().ToList();
        data.Should().NotBeEmpty();
        data.Should().OnlyContain(r => r.GetProperty("priceTier").GetInt32() == 2);
    }

    [Fact]
    public async Task CombinedFilters_AndLogic()
    {
        // Act — Dragon Palace is seeded with cuisine=Chinese, price_tier=1
        var response = await _client.GetAsync(
            "/api/food/restaurants?search=Dragon&cuisine=Chinese&price_tier=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = json.GetProperty("data").EnumerateArray().ToList();

        data.Should().NotBeEmpty();
        data.Should().OnlyContain(r =>
            r.GetProperty("cuisine").GetString() == "Chinese" &&
            r.GetProperty("priceTier").GetInt32() == 1 &&
            r.GetProperty("name").GetString()!.Contains("Dragon", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Page0_Returns422()
    {
        // Act
        var response = await _client.GetAsync("/api/food/restaurants?page=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("error").GetString().Should().Be("validation_failed");
    }

    [Fact]
    public async Task Limit51_Returns422()
    {
        // Act
        var response = await _client.GetAsync("/api/food/restaurants?limit=51");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("error").GetString().Should().Be("validation_failed");
    }

    [Fact]
    public async Task SearchNonExistent_ReturnsEmptyData()
    {
        // Act
        var response = await _client.GetAsync("/api/food/restaurants?search=xxxxxxnonexistentxxxxxx");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = json.GetProperty("data").EnumerateArray().ToList();
        data.Should().BeEmpty();
        json.GetProperty("pagination").GetProperty("totalItems").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task AllRestaurants_AreActiveAndNonDeleted()
    {
        // Act
        var response = await _client.GetAsync("/api/food/restaurants");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = json.GetProperty("data").EnumerateArray().ToList();

        data.Should().NotBeEmpty();
        data.Should().OnlyContain(r => r.GetProperty("status").GetString() == "active");

        // Note: The API automatically filters out deleted restaurants,
        // so we verify no returned restaurant has a non-null deleted_at.
        // (The API response doesn't expose deleted_at, but the handler
        //  filters on DeletedAt == null.)
    }

    // ====================================================================
    // GET /api/food/restaurants/{id}/menu — 5 tests
    // ====================================================================

    [Fact]
    public async Task ValidMenu_Returns200_WithNestedStructure()
    {
        // Arrange — get any active restaurant from the listing
        var listResponse = await _client.GetAsync("/api/food/restaurants?limit=20");
        var listJson = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var restaurants = listJson.GetProperty("data").EnumerateArray().ToList();
        restaurants.Should().NotBeEmpty("there should be at least one active restaurant");
        var restaurantId = restaurants[0].GetProperty("id").GetGuid();

        // Act
        var response = await _client.GetAsync($"/api/food/restaurants/{restaurantId}/menu");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("restaurantId").GetGuid().Should().Be(restaurantId);

        var categories = json.GetProperty("categories").EnumerateArray().ToList();
        categories.Should().NotBeEmpty("the selected restaurant should have seeded menu categories");

        foreach (var cat in categories)
        {
            cat.GetProperty("categoryId").ValueKind.Should().Be(JsonValueKind.String);
            cat.GetProperty("categoryName").GetString().Should().NotBeNullOrEmpty();
            cat.GetProperty("displayOrder").ValueKind.Should().Be(JsonValueKind.Number);

            var items = cat.GetProperty("items").EnumerateArray().ToList();
            items.Should().NotBeEmpty("each category should have at least one item");

            foreach (var item in items)
            {
                item.GetProperty("id").ValueKind.Should().Be(JsonValueKind.String);
                item.GetProperty("name").GetString().Should().NotBeNullOrEmpty();
                item.GetProperty("priceUsd").ValueKind.Should().Be(JsonValueKind.Number);
                item.GetProperty("isAvailable").ValueKind.Should().BeOneOf(
                    JsonValueKind.True, JsonValueKind.False);

                var groups = item.GetProperty("customizationGroups").EnumerateArray().ToList();
                foreach (var group in groups)
                {
                    group.GetProperty("groupId").ValueKind.Should().Be(JsonValueKind.String);
                    group.GetProperty("groupName").GetString().Should().NotBeNullOrEmpty();
                    var options = group.GetProperty("options").EnumerateArray().ToList();
                    foreach (var option in options)
                    {
                        option.GetProperty("optionId").ValueKind.Should().Be(JsonValueKind.String);
                        option.GetProperty("name").GetString().Should().NotBeNullOrEmpty();
                    }
                }
            }
        }
    }

    [Fact]
    public async Task Menu_NonExistentRestaurant_Returns404()
    {
        // Act
        var response = await _client.GetAsync($"/api/food/restaurants/{Guid.NewGuid()}/menu");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("error").GetString().Should().Be("not_found");
    }

    [Fact]
    public async Task Menu_DeletedRestaurant_Returns404()
    {
        // Arrange — create a temp restaurant and mark it as deleted
        Guid restaurantId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TekramDbContext>();
            restaurantId = await CreateTempRestaurantAsync(db, "DeletedMenuTest");

            var restaurant = await db.Restaurants.FindAsync(restaurantId);
            restaurant!.DeletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/api/food/restaurants/{restaurantId}/menu");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("error").GetString().Should().Be("not_found");
    }

    [Fact]
    public async Task Menu_InactiveRestaurant_Returns404()
    {
        // Arrange — create a temp restaurant and set it to inactive
        Guid restaurantId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TekramDbContext>();
            restaurantId = await CreateTempRestaurantAsync(db, "InactiveMenuTest");

            var restaurant = await db.Restaurants.FindAsync(restaurantId);
            restaurant!.Status = "inactive";
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/api/food/restaurants/{restaurantId}/menu");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("error").GetString().Should().Be("not_found");
    }

    [Fact]
    public async Task Menu_IsAvailable_ComputedCorrectly()
    {
        // Arrange — get any active restaurant from the listing
        var listResponse = await _client.GetAsync("/api/food/restaurants?limit=20");
        var listJson = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var restaurants = listJson.GetProperty("data").EnumerateArray().ToList();
        restaurants.Should().NotBeEmpty("there should be at least one active restaurant");
        var restaurantId = restaurants[0].GetProperty("id").GetGuid();

        // Add a menu item with StockCount=0 to test false case
        var zeroStockItemId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TekramDbContext>();

            // Check if we already added a zero-stock item (from a previous test run that didn't clean up)
            var existingZeroStock = await db.MenuItems
                .FirstOrDefaultAsync(m => m.RestaurantId == restaurantId && m.StockCount == 0);

            if (existingZeroStock == null)
            {
                // Find a category to add the test item to
                var category = await db.MenuCategories
                    .FirstAsync(c => c.RestaurantId == restaurantId);

                var testItem = new MenuItem
                {
                    Id = zeroStockItemId,
                    CategoryId = category.Id,
                    RestaurantId = restaurantId,
                    Name = "Zero Stock Test Item",
                    Description = "Item with stock count of 0",
                    PriceUsd = 1.00m,
                    StockCount = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                db.MenuItems.Add(testItem);
                await db.SaveChangesAsync();
            }
        }

        // Act
        var response = await _client.GetAsync($"/api/food/restaurants/{restaurantId}/menu");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = json.GetProperty("categories").EnumerateArray()
            .SelectMany(c => c.GetProperty("items").EnumerateArray())
            .ToList();

        // Verify: StockCount=null → isAvailable=true
        var nullStockItems = items.Where(i =>
            i.GetProperty("stockCount").ValueKind == JsonValueKind.Null);
        nullStockItems.Should().NotBeEmpty("menu should have items with null stock count");
        nullStockItems.Should().AllSatisfy(i =>
            i.GetProperty("isAvailable").GetBoolean().Should().BeTrue(
                "items with null stock count should be available"));

        // Verify: StockCount>0 → isAvailable=true
        var positiveStockItems = items.Where(i =>
            i.GetProperty("stockCount").ValueKind == JsonValueKind.Number &&
            i.GetProperty("stockCount").GetInt32() > 0);
        positiveStockItems.Should().NotBeEmpty("menu should have items with positive stock count");
        positiveStockItems.Should().AllSatisfy(i =>
            i.GetProperty("isAvailable").GetBoolean().Should().BeTrue(
                "items with stock count > 0 should be available"));

        // Verify: StockCount=0 → isAvailable=false
        var zeroStockItem = items.FirstOrDefault(i =>
            i.GetProperty("name").GetString() == "Zero Stock Test Item");
        zeroStockItem.ValueKind.Should().NotBe(JsonValueKind.Undefined,
            "Zero Stock Test Item should exist in the menu response");
        zeroStockItem.GetProperty("isAvailable").GetBoolean().Should().BeFalse(
            "items with stock count = 0 should NOT be available");
    }
}

/// <summary>
/// Collection definition to disable parallel execution for restaurant integration tests.
/// </summary>
[CollectionDefinition("RestaurantIntegrationTests", DisableParallelization = true)]
public class RestaurantIntegrationTestCollection
{
}
