namespace Tekram.Api.src.shared;

using Tekram.Api.src.orders.Domain;
using Tekram.Api.src.restaurants.Domain;

public static class DbInitializer
{
    public static async Task SeedAsync(TekramDbContext db)
    {
        // ---- Backfill edge-case coupons on existing databases ----
        // Guard each individually by Code so partial deploys still get backfilled
        var distantPast = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var past = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        if (!db.Coupons.Any(c => c.Code == "EXPIRED50"))
        {
            db.Coupons.Add(new Coupon
            {
                Id = Guid.NewGuid(), Code = "EXPIRED50", DiscountType = "percent",
                DiscountValue = 50m, MinSubtotalUsd = 0m, MaxUses = null,
                UsesCount = 0, ValidFrom = distantPast, ValidUntil = past, Active = false
            });
        }

        if (!db.Coupons.Any(c => c.Code == "BIGSPENDER"))
        {
            db.Coupons.Add(new Coupon
            {
                Id = Guid.NewGuid(), Code = "BIGSPENDER", DiscountType = "percent",
                DiscountValue = 20m, MinSubtotalUsd = 100m, MaxUses = 5,
                UsesCount = 0, ValidFrom = distantPast,
                ValidUntil = new DateTime(2030, 12, 31, 23, 59, 59, DateTimeKind.Utc), Active = true
            });
        }

        // Persist backfill BEFORE the early-exit so existing DBs get the new coupons.
        // (The bulk seed below is guarded by Restaurants.Any(), which returns early
        //  on already-seeded databases — SaveChangesAsync never reached otherwise.)
        await db.SaveChangesAsync();

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
        var caprese = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = cat1.Id, RestaurantId = r1.Id,
            Name = "Caprese Salad", Description = "Fresh mozzarella, tomatoes, and basil.",
            PriceUsd = 6.00m, StockCount = null
        };
        var calamari = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = cat1.Id, RestaurantId = r1.Id,
            Name = "Fried Calamari", Description = "Crispy calamari with marinara dip.",
            PriceUsd = 7.00m, StockCount = null
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

        var quattroFormaggi = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = cat2.Id, RestaurantId = r1.Id,
            Name = "Quattro Formaggi", Description = "Four-cheese pizza with gorgonzola.",
            PriceUsd = 10.50m, StockCount = null
        };

        var cola = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = cat3.Id, RestaurantId = r1.Id,
            Name = "Cola", Description = "330ml can",
            PriceUsd = 1.50m, StockCount = null
        };
        var sanPellegrino = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = cat3.Id, RestaurantId = r1.Id,
            Name = "San Pellegrino", Description = "Sparkling mineral water.",
            PriceUsd = 2.00m, StockCount = null
        };
        var espresso = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = cat3.Id, RestaurantId = r1.Id,
            Name = "Espresso", Description = "Double shot Italian espresso.",
            PriceUsd = 2.50m, StockCount = null
        };

        db.MenuItems.AddRange(bruschetta, caprese, calamari,
            margherita, pepperoni, quattroFormaggi,
            cola, sanPellegrino, espresso);

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
        var cheeseburger = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = bc1.Id, RestaurantId = r2.Id,
            Name = "Cheeseburger", Description = "Classic beef patty with cheddar cheese.",
            PriceUsd = 7.00m, StockCount = null
        };
        var chickenBurgerBN = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = bc1.Id, RestaurantId = r2.Id,
            Name = "Chicken Burger", Description = "Grilled chicken breast with avocado.",
            PriceUsd = 7.50m, StockCount = 10
        };

        var fries = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = bc2.Id, RestaurantId = r2.Id,
            Name = "French Fries", Description = "Golden crispy fries.",
            PriceUsd = 3.00m, StockCount = null
        };
        var onionRings = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = bc2.Id, RestaurantId = r2.Id,
            Name = "Onion Rings", Description = "Beer-battered onion rings.",
            PriceUsd = 3.50m, StockCount = null
        };
        var coleslaw = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = bc2.Id, RestaurantId = r2.Id,
            Name = "Coleslaw", Description = "Creamy cabbage and carrot slaw.",
            PriceUsd = 2.50m, StockCount = null
        };

        db.MenuItems.AddRange(classicBurger, cheeseburger, chickenBurgerBN,
            fries, onionRings, coleslaw);

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

        var spicyTunaRoll = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = sc1.Id, RestaurantId = r3.Id,
            Name = "Spicy Tuna Roll", Description = "Fresh tuna with spicy mayo and cucumber.",
            PriceUsd = 10.00m, StockCount = 8
        };

        var salmonSashimi = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = sc2.Id, RestaurantId = r3.Id,
            Name = "Salmon Sashimi", Description = "Fresh Norwegian salmon sliced to perfection.",
            PriceUsd = 14.00m, StockCount = 10
        };

        var tunaSashimi = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = sc2.Id, RestaurantId = r3.Id,
            Name = "Tuna Sashimi", Description = "Premium bluefin tuna, thinly sliced.",
            PriceUsd = 16.00m, StockCount = 6
        };

        var yellowtailSashimi = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = sc2.Id, RestaurantId = r3.Id,
            Name = "Yellowtail Sashimi", Description = "Hamachi with ponzu and jalapeño.",
            PriceUsd = 15.00m, StockCount = null
        };

        var greenTea = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = sc3.Id, RestaurantId = r3.Id,
            Name = "Green Tea", Description = "Traditional Japanese green tea.",
            PriceUsd = 2.50m, StockCount = null
        };

        var matchaLatte = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = sc3.Id, RestaurantId = r3.Id,
            Name = "Matcha Latte", Description = "Creamy matcha with steamed milk.",
            PriceUsd = 4.00m, StockCount = null
        };

        var ramune = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = sc3.Id, RestaurantId = r3.Id,
            Name = "Ramune", Description = "Japanese marble soda, classic flavor.",
            PriceUsd = 3.00m, StockCount = 12
        };

        db.MenuItems.AddRange(californiaRoll, dragonRoll, spicyTunaRoll,
            salmonSashimi, tunaSashimi, yellowtailSashimi,
            greenTea, matchaLatte, ramune);

        // ---- Restaurant: Beirut Grill ----
        var r4 = new Restaurant
        {
            Id = Guid.NewGuid(), Name = "Beirut Grill",
            Description = "Authentic Lebanese mezza and grilled specialties.",
            Cuisine = "Lebanese", Rating = 4.5m, PriceTier = 2, AvgPrepMinutes = 25,
            Latitude = 33.8930m, Longitude = 35.5060m, Status = "active"
        };
        db.Restaurants.Add(r4);

        var bgc1 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r4.Id, Name = "Mezza", DisplayOrder = 1 };
        var bgc2 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r4.Id, Name = "Grill", DisplayOrder = 2 };
        db.MenuCategories.AddRange(bgc1, bgc2);

        var hummus = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = bgc1.Id, RestaurantId = r4.Id,
            Name = "Hummus", Description = "Creamy chickpea dip with tahini and olive oil.",
            PriceUsd = 4.50m, StockCount = null
        };
        var tabbouleh = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = bgc1.Id, RestaurantId = r4.Id,
            Name = "Tabbouleh", Description = "Fresh parsley salad with bulgur and lemon dressing.",
            PriceUsd = 5.00m, StockCount = null
        };
        var falafel = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = bgc1.Id, RestaurantId = r4.Id,
            Name = "Falafel", Description = "Crispy chickpea patties served with tahini sauce.",
            PriceUsd = 4.00m, StockCount = null
        };
        var shishTaouk = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = bgc2.Id, RestaurantId = r4.Id,
            Name = "Shish Taouk", Description = "Marinated chicken skewers with garlic sauce.",
            PriceUsd = 9.00m, StockCount = null
        };
        var kafta = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = bgc2.Id, RestaurantId = r4.Id,
            Name = "Kafta", Description = "Spiced ground lamb skewers with grilled vegetables.",
            PriceUsd = 8.50m, StockCount = 12
        };
        var mixedGrill = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = bgc2.Id, RestaurantId = r4.Id,
            Name = "Mixed Grill", Description = "Assortment of grilled meats with hummus and bread.",
            PriceUsd = 12.00m, StockCount = null
        };
        db.MenuItems.AddRange(hummus, tabbouleh, falafel, shishTaouk, kafta, mixedGrill);

        var spiceGroupTaouk = new CustomizationGroup
        {
            Id = Guid.NewGuid(), MenuItemId = shishTaouk.Id,
            Name = "Spice Level", IsRequired = true, MaxSelections = 1
        };
        db.CustomizationGroups.Add(spiceGroupTaouk);
        db.CustomizationOptions.AddRange(
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = spiceGroupTaouk.Id, Name = "Mild", PriceModifierUsd = 0m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = spiceGroupTaouk.Id, Name = "Medium", PriceModifierUsd = 0m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = spiceGroupTaouk.Id, Name = "Hot", PriceModifierUsd = 0m }
        );

        // ---- Restaurant: Dragon Palace ----
        var r5 = new Restaurant
        {
            Id = Guid.NewGuid(), Name = "Dragon Palace",
            Description = "Classic Chinese cuisine from dim sum to main dishes.",
            Cuisine = "Chinese", Rating = 4.3m, PriceTier = 1, AvgPrepMinutes = 20,
            Latitude = 33.8950m, Longitude = 35.5100m, Status = "active"
        };
        db.Restaurants.Add(r5);

        var dpc1 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r5.Id, Name = "Dim Sum", DisplayOrder = 1 };
        var dpc2 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r5.Id, Name = "Main Dishes", DisplayOrder = 2 };
        db.MenuCategories.AddRange(dpc1, dpc2);

        var springRolls = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = dpc1.Id, RestaurantId = r5.Id,
            Name = "Spring Rolls", Description = "Crispy vegetable spring rolls with sweet chili dip.",
            PriceUsd = 3.50m, StockCount = 12
        };
        var dumplings = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = dpc1.Id, RestaurantId = r5.Id,
            Name = "Dumplings", Description = "Steamed pork and vegetable dumplings.",
            PriceUsd = 5.00m, StockCount = null
        };
        var wontonSoup = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = dpc1.Id, RestaurantId = r5.Id,
            Name = "Wonton Soup", Description = "Savory broth with pork-filled wontons.",
            PriceUsd = 4.50m, StockCount = null
        };
        var kungPaoChicken = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = dpc2.Id, RestaurantId = r5.Id,
            Name = "Kung Pao Chicken", Description = "Stir-fried chicken with peanuts and chili.",
            PriceUsd = 8.00m, StockCount = null
        };
        var sweetSourPork = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = dpc2.Id, RestaurantId = r5.Id,
            Name = "Sweet & Sour Pork", Description = "Crispy pork in tangy sweet and sour sauce.",
            PriceUsd = 7.50m, StockCount = null
        };
        var friedRice = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = dpc2.Id, RestaurantId = r5.Id,
            Name = "Fried Rice", Description = "Wok-fried rice with egg, peas, and scallions.",
            PriceUsd = 5.00m, StockCount = 10
        };
        db.MenuItems.AddRange(springRolls, dumplings, wontonSoup, kungPaoChicken, sweetSourPork, friedRice);

        // ---- Restaurant: Cantina Del Sol ----
        var r6 = new Restaurant
        {
            Id = Guid.NewGuid(), Name = "Cantina Del Sol",
            Description = "Vibrant Mexican flavors from tacos to burritos.",
            Cuisine = "Mexican", Rating = 4.6m, PriceTier = 2, AvgPrepMinutes = 22,
            Latitude = 33.8880m, Longitude = 35.5200m, Status = "active"
        };
        db.Restaurants.Add(r6);

        var cds1 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r6.Id, Name = "Tacos", DisplayOrder = 1 };
        var cds2 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r6.Id, Name = "Burritos", DisplayOrder = 2 };
        db.MenuCategories.AddRange(cds1, cds2);

        var alPastorTaco = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = cds1.Id, RestaurantId = r6.Id,
            Name = "Al Pastor Taco", Description = "Marinated pork with pineapple and cilantro.",
            PriceUsd = 3.50m, StockCount = 10
        };
        var carnitasTaco = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = cds1.Id, RestaurantId = r6.Id,
            Name = "Carnitas Taco", Description = "Slow-cooked pork with onion and lime.",
            PriceUsd = 3.50m, StockCount = null
        };
        var fishTaco = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = cds1.Id, RestaurantId = r6.Id,
            Name = "Fish Taco", Description = "Beer-battered fish with slaw and chipotle mayo.",
            PriceUsd = 4.00m, StockCount = null
        };
        var beanBurrito = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = cds2.Id, RestaurantId = r6.Id,
            Name = "Bean Burrito", Description = "Refried beans, cheese, and rice in a flour tortilla.",
            PriceUsd = 6.00m, StockCount = null
        };
        var chickenBurrito = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = cds2.Id, RestaurantId = r6.Id,
            Name = "Chicken Burrito", Description = "Grilled chicken with guacamole and pico de gallo.",
            PriceUsd = 7.00m, StockCount = null
        };
        var steakBurrito = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = cds2.Id, RestaurantId = r6.Id,
            Name = "Steak Burrito", Description = "Grilled steak with black beans and queso.",
            PriceUsd = 8.50m, StockCount = 8
        };
        db.MenuItems.AddRange(alPastorTaco, carnitasTaco, fishTaco, beanBurrito, chickenBurrito, steakBurrito);

        var toppingsGroup = new CustomizationGroup
        {
            Id = Guid.NewGuid(), MenuItemId = alPastorTaco.Id,
            Name = "Toppings", IsRequired = false, MaxSelections = 3
        };
        db.CustomizationGroups.Add(toppingsGroup);
        db.CustomizationOptions.AddRange(
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = toppingsGroup.Id, Name = "Cilantro", PriceModifierUsd = 0m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = toppingsGroup.Id, Name = "Onion", PriceModifierUsd = 0m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = toppingsGroup.Id, Name = "Guacamole", PriceModifierUsd = 1.50m }
        );

        // ---- Restaurant: Taj Mahal Palace ----
        var r7 = new Restaurant
        {
            Id = Guid.NewGuid(), Name = "Taj Mahal Palace",
            Description = "Rich and aromatic Indian curries and biryani.",
            Cuisine = "Indian", Rating = 4.7m, PriceTier = 3, AvgPrepMinutes = 30,
            Latitude = 33.8970m, Longitude = 35.5030m, Status = "active"
        };
        db.Restaurants.Add(r7);

        var tmc1 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r7.Id, Name = "Curries", DisplayOrder = 1 };
        var tmc2 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r7.Id, Name = "Biryani", DisplayOrder = 2 };
        db.MenuCategories.AddRange(tmc1, tmc2);

        var butterChicken = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = tmc1.Id, RestaurantId = r7.Id,
            Name = "Butter Chicken", Description = "Tender chicken in a creamy tomato sauce.",
            PriceUsd = 10.00m, StockCount = null
        };
        var palakPaneer = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = tmc1.Id, RestaurantId = r7.Id,
            Name = "Palak Paneer", Description = "Cottage cheese cubes in a spiced spinach gravy.",
            PriceUsd = 9.00m, StockCount = null
        };
        var lambRoganJosh = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = tmc1.Id, RestaurantId = r7.Id,
            Name = "Lamb Rogan Josh", Description = "Slow-cooked lamb in aromatic Kashmiri spices.",
            PriceUsd = 11.50m, StockCount = null
        };
        var chickenBiryani = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = tmc2.Id, RestaurantId = r7.Id,
            Name = "Chicken Biryani", Description = "Fragrant basmati rice layered with spiced chicken.",
            PriceUsd = 10.50m, StockCount = null
        };
        var vegBiryani = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = tmc2.Id, RestaurantId = r7.Id,
            Name = "Veg Biryani", Description = "Aromatic rice with seasonal vegetables and saffron.",
            PriceUsd = 8.50m, StockCount = 7
        };
        var lambBiryani = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = tmc2.Id, RestaurantId = r7.Id,
            Name = "Lamb Biryani", Description = "Succulent lamb cooked with basmati rice and spices.",
            PriceUsd = 12.00m, StockCount = null
        };
        db.MenuItems.AddRange(butterChicken, palakPaneer, lambRoganJosh, chickenBiryani, vegBiryani, lambBiryani);

        // ---- Restaurant: Le Petit Bistro ----
        var r8 = new Restaurant
        {
            Id = Guid.NewGuid(), Name = "Le Petit Bistro",
            Description = "Classic French cuisine in a cozy bistro setting.",
            Cuisine = "French", Rating = 4.4m, PriceTier = 3, AvgPrepMinutes = 28,
            Latitude = 33.8900m, Longitude = 35.5150m, Status = "active"
        };
        db.Restaurants.Add(r8);

        var lpc1 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r8.Id, Name = "Entrées", DisplayOrder = 1 };
        var lpc2 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r8.Id, Name = "Plats", DisplayOrder = 2 };
        db.MenuCategories.AddRange(lpc1, lpc2);

        var frenchOnionSoup = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = lpc1.Id, RestaurantId = r8.Id,
            Name = "French Onion Soup", Description = "Caramelized onion soup with gruyère crouton.",
            PriceUsd = 6.50m, StockCount = null
        };
        var saladeNicoise = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = lpc1.Id, RestaurantId = r8.Id,
            Name = "Salade Niçoise", Description = "Tuna, olives, eggs, and anchovies over fresh greens.",
            PriceUsd = 7.00m, StockCount = null
        };
        var escargots = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = lpc1.Id, RestaurantId = r8.Id,
            Name = "Escargots", Description = "Baked snails with garlic-parsley butter.",
            PriceUsd = 9.00m, StockCount = 5
        };
        var coqAuVin = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = lpc2.Id, RestaurantId = r8.Id,
            Name = "Coq au Vin", Description = "Braised chicken in red wine with mushrooms.",
            PriceUsd = 12.50m, StockCount = null
        };
        var steakFrites = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = lpc2.Id, RestaurantId = r8.Id,
            Name = "Steak Frites", Description = "Pan-seared steak with pommes frites and béarnaise.",
            PriceUsd = 14.00m, StockCount = null
        };
        var ratatouille = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = lpc2.Id, RestaurantId = r8.Id,
            Name = "Ratatouille", Description = "Provençal vegetable stew with herbs de Provence.",
            PriceUsd = 8.00m, StockCount = null
        };
        db.MenuItems.AddRange(frenchOnionSoup, saladeNicoise, escargots, coqAuVin, steakFrites, ratatouille);

        // ---- Restaurant: Greek Taverna ----
        var r9 = new Restaurant
        {
            Id = Guid.NewGuid(), Name = "Greek Taverna",
            Description = "Hearty Greek dishes from the Mediterranean coast.",
            Cuisine = "Greek", Rating = 4.0m, PriceTier = 1, AvgPrepMinutes = 18,
            Latitude = 33.8920m, Longitude = 35.5080m, Status = "active"
        };
        db.Restaurants.Add(r9);

        var gtc1 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r9.Id, Name = "Starters", DisplayOrder = 1 };
        var gtc2 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r9.Id, Name = "Mains", DisplayOrder = 2 };
        db.MenuCategories.AddRange(gtc1, gtc2);

        var tzatziki = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = gtc1.Id, RestaurantId = r9.Id,
            Name = "Tzatziki", Description = "Yogurt with cucumber, garlic, and dill.",
            PriceUsd = 3.00m, StockCount = null
        };
        var spanakopita = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = gtc1.Id, RestaurantId = r9.Id,
            Name = "Spanakopita", Description = "Flaky phyllo pastry filled with spinach and feta.",
            PriceUsd = 4.50m, StockCount = null
        };
        var greekSalad = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = gtc1.Id, RestaurantId = r9.Id,
            Name = "Greek Salad", Description = "Tomatoes, cucumber, olives, feta, and oregano.",
            PriceUsd = 5.50m, StockCount = null
        };
        var souvlaki = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = gtc2.Id, RestaurantId = r9.Id,
            Name = "Souvlaki", Description = "Grilled meat skewers with pita and tzatziki.",
            PriceUsd = 7.00m, StockCount = 10
        };
        var moussaka = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = gtc2.Id, RestaurantId = r9.Id,
            Name = "Moussaka", Description = "Layered eggplant, minced meat, and béchamel sauce.",
            PriceUsd = 8.50m, StockCount = null
        };
        var gyroPlate = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = gtc2.Id, RestaurantId = r9.Id,
            Name = "Gyro Plate", Description = "Sliced rotisserie meat with fries and salad.",
            PriceUsd = 7.50m, StockCount = null
        };
        db.MenuItems.AddRange(tzatziki, spanakopita, greekSalad, souvlaki, moussaka, gyroPlate);

        var addonsGroup = new CustomizationGroup
        {
            Id = Guid.NewGuid(), MenuItemId = souvlaki.Id,
            Name = "Add-ons", IsRequired = false, MaxSelections = 2
        };
        db.CustomizationGroups.Add(addonsGroup);
        db.CustomizationOptions.AddRange(
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = addonsGroup.Id, Name = "Pita Bread", PriceModifierUsd = 0.50m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = addonsGroup.Id, Name = "Extra Tzatziki", PriceModifierUsd = 1.00m }
        );

        // ---- Restaurant: Seoul Kitchen ----
        var r10 = new Restaurant
        {
            Id = Guid.NewGuid(), Name = "Seoul Kitchen",
            Description = "Korean BBQ and hearty stews with bold flavors.",
            Cuisine = "Korean", Rating = 4.2m, PriceTier = 4, AvgPrepMinutes = 24,
            Latitude = 33.8940m, Longitude = 35.5050m, Status = "active"
        };
        db.Restaurants.Add(r10);

        var skc1 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r10.Id, Name = "BBQ", DisplayOrder = 1 };
        var skc2 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r10.Id, Name = "Soups & Stews", DisplayOrder = 2 };
        db.MenuCategories.AddRange(skc1, skc2);

        var bulgogi = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = skc1.Id, RestaurantId = r10.Id,
            Name = "Bulgogi", Description = "Marinated grilled beef with ssamjang.",
            PriceUsd = 9.50m, StockCount = null
        };
        var samgyeopsal = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = skc1.Id, RestaurantId = r10.Id,
            Name = "Samgyeopsal", Description = "Grilled pork belly with lettuce wraps.",
            PriceUsd = 10.00m, StockCount = 15
        };
        var galbi = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = skc1.Id, RestaurantId = r10.Id,
            Name = "Galbi", Description = "Soy-marinated beef short ribs, grilled to order.",
            PriceUsd = 12.50m, StockCount = null
        };
        var kimchiJjigae = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = skc2.Id, RestaurantId = r10.Id,
            Name = "Kimchi Jjigae", Description = "Spicy kimchi stew with pork and tofu.",
            PriceUsd = 7.00m, StockCount = null
        };
        var sundubu = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = skc2.Id, RestaurantId = r10.Id,
            Name = "Sundubu", Description = "Soft tofu stew with seafood and egg.",
            PriceUsd = 8.00m, StockCount = null
        };
        var bibimbap = new MenuItem
        {
            Id = Guid.NewGuid(), CategoryId = skc2.Id, RestaurantId = r10.Id,
            Name = "Bibimbap", Description = "Mixed rice bowl with vegetables, egg, and gochujang.",
            PriceUsd = 8.50m, StockCount = 8
        };
        db.MenuItems.AddRange(bulgogi, samgyeopsal, galbi, kimchiJjigae, sundubu, bibimbap);

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
