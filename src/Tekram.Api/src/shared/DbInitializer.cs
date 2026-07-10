namespace Tekram.Api.src.shared;

using Tekram.Api.src.orders.Domain;
using Tekram.Api.src.restaurants.Domain;

public static class DbInitializer
{
    public static async Task SeedAsync(TekramDbContext db)
    {
        if (db.Restaurants.Any()) return; // Already seeded

        // ---- Restaurants ----
        var r1 = new Restaurant
        {
            Id = Guid.NewGuid(), Name = "La Trattoria",
            Description = "Authentic Italian wood-fired pizza and pasta.",
            Cuisine = "Italian", Rating = 4.7m, PriceTier = 2, AvgPrepMinutes = 35,
            Latitude = 33.8892m, Longitude = 35.5184m, Status = "active"
        };

        var r2 = new Restaurant
        {
            Id = Guid.NewGuid(), Name = "Burger Nation",
            Description = "Gourmet burgers with a Lebanese twist.",
            Cuisine = "Burgers", Rating = 4.3m, PriceTier = 2, AvgPrepMinutes = 20,
            Latitude = 33.8938m, Longitude = 35.5024m, Status = "active"
        };

        var r3 = new Restaurant
        {
            Id = Guid.NewGuid(), Name = "Sushi Zen",
            Description = "Premium Japanese sushi and sashimi.",
            Cuisine = "Japanese", Rating = 4.8m, PriceTier = 3, AvgPrepMinutes = 40,
            Latitude = 33.9000m, Longitude = 35.4950m, Status = "active"
        };

        db.Restaurants.AddRange(r1, r2, r3);

        // ---- Menu Categories for La Trattoria ----
        var cat1 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r1.Id, Name = "Starters", DisplayOrder = 1 };
        var cat2 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r1.Id, Name = "Pizzas", DisplayOrder = 2 };
        var cat3 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r1.Id, Name = "Beverages", DisplayOrder = 3 };
        db.MenuCategories.AddRange(cat1, cat2, cat3);

        // ---- Menu Items ----
        var bruschetta = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = cat1.Id, RestaurantId = r1.Id,
            Name = "Bruschetta", Description = "Toasted bread with tomato and basil.",
            PriceUsd = 5.50m, StockCount = null
        };

        var margherita = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = cat2.Id, RestaurantId = r1.Id,
            Name = "Margherita Pizza",
            Description = "Fresh tomato sauce, mozzarella, and basil.",
            PriceUsd = 7.50m, StockCount = null
        };

        var pepperoni = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = cat2.Id, RestaurantId = r1.Id,
            Name = "Pepperoni Pizza",
            Description = "Classic pepperoni with mozzarella.",
            PriceUsd = 9.00m, StockCount = 20
        };

        var cola = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = cat3.Id, RestaurantId = r1.Id,
            Name = "Cola", Description = "330ml can",
            PriceUsd = 1.50m, StockCount = null
        };

        db.MenuItems.AddRange(bruschetta, margherita, pepperoni, cola);

        // ---- Customization: Pizza Size ----
        var sizeGroupMarg = new CustomizationGroup
        {
            Id = Guid.NewGuid(), MenuItemId = margherita.Id,
            Name = "Size", IsRequired = true, MaxSelections = 1
        };
        var sizeGroupPep = new CustomizationGroup
        {
            Id = Guid.NewGuid(), MenuItemId = pepperoni.Id,
            Name = "Size", IsRequired = true, MaxSelections = 1
        };
        db.CustomizationGroups.AddRange(sizeGroupMarg, sizeGroupPep);

        db.CustomizationOptions.AddRange(
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = sizeGroupMarg.Id, Name = "Medium", PriceModifierUsd = 0m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = sizeGroupMarg.Id, Name = "Large", PriceModifierUsd = 2.50m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = sizeGroupPep.Id, Name = "Medium", PriceModifierUsd = 0m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = sizeGroupPep.Id, Name = "Large", PriceModifierUsd = 3.00m }
        );

        // ---- Menu Categories for Burger Nation ----
        var bc1 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r2.Id, Name = "Burgers", DisplayOrder = 1 };
        var bc2 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r2.Id, Name = "Sides", DisplayOrder = 2 };
        db.MenuCategories.AddRange(bc1, bc2);

        var classicBurger = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = bc1.Id, RestaurantId = r2.Id,
            Name = "Classic Burger", Description = "Beef patty, lettuce, tomato, special sauce.",
            PriceUsd = 6.00m, StockCount = null
        };

        var fries = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = bc2.Id, RestaurantId = r2.Id,
            Name = "French Fries", Description = "Golden crispy fries.",
            PriceUsd = 3.00m, StockCount = null
        };

        db.MenuItems.AddRange(classicBurger, fries);

        // ---- Menu Categories for Sushi Zen ----
        var sc1 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r3.Id, Name = "Sushi Rolls", DisplayOrder = 1 };
        var sc2 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r3.Id, Name = "Sashimi", DisplayOrder = 2 };
        var sc3 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r3.Id, Name = "Beverages", DisplayOrder = 3 };
        db.MenuCategories.AddRange(sc1, sc2, sc3);

        var californiaRoll = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = sc1.Id, RestaurantId = r3.Id,
            Name = "California Roll", Description = "Crab, avocado, cucumber inside-out roll.",
            PriceUsd = 8.50m, StockCount = null
        };

        var dragonRoll = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = sc1.Id, RestaurantId = r3.Id,
            Name = "Dragon Roll", Description = "Shrimp tempura, eel, avocado drizzle.",
            PriceUsd = 12.00m, StockCount = 15
        };

        var salmonSashimi = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = sc2.Id, RestaurantId = r3.Id,
            Name = "Salmon Sashimi", Description = "Fresh Norwegian salmon sliced to perfection.",
            PriceUsd = 14.00m, StockCount = 10
        };

        var greenTea = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = sc3.Id, RestaurantId = r3.Id,
            Name = "Green Tea", Description = "Traditional Japanese green tea.",
            PriceUsd = 2.50m, StockCount = null
        };

        db.MenuItems.AddRange(californiaRoll, dragonRoll, salmonSashimi, greenTea);

        // ---- Coupons ----
        var now = DateTime.UtcNow;
        db.Coupons.AddRange(
            new Coupon
            {
                Id = Guid.NewGuid(),                 Code = "WELCOME10", DiscountType = "percent",
                DiscountValue = 10m, MinSubtotalUsd = 10m, MaxUses = 100,
                UsesCount = 0, ValidFrom = now.AddDays(-30), ValidUntil = now.AddDays(60), Active = true
            },
            new Coupon
            {
                Id = Guid.NewGuid(),                 Code = "FREEDELIVERY", DiscountType = "fixed",
                DiscountValue = 1.50m, MinSubtotalUsd = 5m, MaxUses = 500,
                UsesCount = 0, ValidFrom = now.AddDays(-30), ValidUntil = now.AddDays(30), Active = true
            }
        );

        await db.SaveChangesAsync();
    }
}
