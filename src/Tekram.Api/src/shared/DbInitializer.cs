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

        // ---- Restaurants 4–10 (Extended seed for pagination testing) ----

        // ---- Beirut Grill (Lebanese) ----
        var r4 = new Restaurant
        {
            Id = Guid.NewGuid(), Name = "Beirut Grill",
            Description = "Authentic Lebanese mezza and grilled specialties.",
            Cuisine = "Lebanese", Rating = 4.5m, PriceTier = 2, AvgPrepMinutes = 30,
            Latitude = 33.8860m, Longitude = 35.4820m, Status = "active"
        };

        var r5 = new Restaurant
        {
            Id = Guid.NewGuid(), Name = "Dragon Palace",
            Description = "Traditional Chinese cuisine from Szechuan to Cantonese.",
            Cuisine = "Chinese", Rating = 4.2m, PriceTier = 1, AvgPrepMinutes = 25,
            Latitude = 33.8905m, Longitude = 35.4760m, Status = "active"
        };

        var r6 = new Restaurant
        {
            Id = Guid.NewGuid(), Name = "Cantina Del Sol",
            Description = "Vibrant Mexican tacos, burritos, and more.",
            Cuisine = "Mexican", Rating = 4.0m, PriceTier = 1, AvgPrepMinutes = 20,
            Latitude = 33.8845m, Longitude = 35.4900m, Status = "active"
        };

        var r7 = new Restaurant
        {
            Id = Guid.NewGuid(), Name = "Taj Mahal Palace",
            Description = "Rich Indian curries and tandoori classics.",
            Cuisine = "Indian", Rating = 4.6m, PriceTier = 3, AvgPrepMinutes = 40,
            Latitude = 33.8920m, Longitude = 35.5000m, Status = "active"
        };

        var r8 = new Restaurant
        {
            Id = Guid.NewGuid(), Name = "Le Petit Bistro",
            Description = "Elegant French dining with timeless recipes.",
            Cuisine = "French", Rating = 4.8m, PriceTier = 4, AvgPrepMinutes = 45,
            Latitude = 33.8880m, Longitude = 35.5100m, Status = "active"
        };

        var r9 = new Restaurant
        {
            Id = Guid.NewGuid(), Name = "The Greek Taverna",
            Description = "Fresh Mediterranean flavours and Greek hospitality.",
            Cuisine = "Greek", Rating = 4.4m, PriceTier = 2, AvgPrepMinutes = 30,
            Latitude = 33.8960m, Longitude = 35.5050m, Status = "active"
        };

        var r10 = new Restaurant
        {
            Id = Guid.NewGuid(), Name = "Seoul Kitchen",
            Description = "Korean BBQ and bibimbap bowls.",
            Cuisine = "Korean", Rating = 4.3m, PriceTier = 3, AvgPrepMinutes = 35,
            Latitude = 33.8980m, Longitude = 35.4880m, Status = "active"
        };

        db.Restaurants.AddRange(r4, r5, r6, r7, r8, r9, r10);

        // ======== Beirut Grill Menu ========
        var lcat1 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r4.Id, Name = "Mezza", DisplayOrder = 1 };
        var lcat2 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r4.Id, Name = "Grills", DisplayOrder = 2 };
        var lcat3 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r4.Id, Name = "Desserts", DisplayOrder = 3 };
        db.MenuCategories.AddRange(lcat1, lcat2, lcat3);

        var hummus = new MenuItem { Id = Guid.NewGuid(), CategoryId = lcat1.Id, RestaurantId = r4.Id, Name = "Hummus", Description = "Chickpea dip with tahini and olive oil.", PriceUsd = 4.50m, StockCount = null };
        var tabbouleh = new MenuItem { Id = Guid.NewGuid(), CategoryId = lcat1.Id, RestaurantId = r4.Id, Name = "Tabbouleh", Description = "Parsley and bulgur wheat salad.", PriceUsd = 4.00m, StockCount = null };
        var falafel = new MenuItem { Id = Guid.NewGuid(), CategoryId = lcat1.Id, RestaurantId = r4.Id, Name = "Falafel", Description = "Crispy chickpea fritters with tahini.", PriceUsd = 5.00m, StockCount = 15 };
        var grapeLeaves = new MenuItem { Id = Guid.NewGuid(), CategoryId = lcat1.Id, RestaurantId = r4.Id, Name = "Stuffed Grape Leaves", Description = "Rice and herb stuffed vine leaves.", PriceUsd = 5.50m, StockCount = null };
        var chickenShawarma = new MenuItem { Id = Guid.NewGuid(), CategoryId = lcat2.Id, RestaurantId = r4.Id, Name = "Chicken Shawarma", Description = "Marinated chicken with garlic sauce.", PriceUsd = 7.50m, StockCount = null };
        var beefKebab = new MenuItem { Id = Guid.NewGuid(), CategoryId = lcat2.Id, RestaurantId = r4.Id, Name = "Beef Kebab", Description = "Grilled seasoned beef skewers.", PriceUsd = 9.00m, StockCount = null };
        var mixedGrill = new MenuItem { Id = Guid.NewGuid(), CategoryId = lcat2.Id, RestaurantId = r4.Id, Name = "Mixed Grill", Description = "Assorted grilled meats with rice.", PriceUsd = 11.00m, StockCount = 10 };
        var baklava = new MenuItem { Id = Guid.NewGuid(), CategoryId = lcat3.Id, RestaurantId = r4.Id, Name = "Baklava", Description = "Layered pastry with pistachio and honey.", PriceUsd = 4.00m, StockCount = null };
        var knafeh = new MenuItem { Id = Guid.NewGuid(), CategoryId = lcat3.Id, RestaurantId = r4.Id, Name = "Knafeh", Description = "Cheese pastry with sweet syrup.", PriceUsd = 5.50m, StockCount = null };
        db.MenuItems.AddRange(hummus, tabbouleh, falafel, grapeLeaves, chickenShawarma, beefKebab, mixedGrill, baklava, knafeh);

        // Customizations: Shawarma size + spice level
        var shawarmaSizeGroup = new CustomizationGroup { Id = Guid.NewGuid(), MenuItemId = chickenShawarma.Id, Name = "Size", IsRequired = true, MaxSelections = 1 };
        var shawarmaSpiceGroup = new CustomizationGroup { Id = Guid.NewGuid(), MenuItemId = chickenShawarma.Id, Name = "Spice Level", IsRequired = false, MaxSelections = 1 };
        db.CustomizationGroups.AddRange(shawarmaSizeGroup, shawarmaSpiceGroup);
        db.CustomizationOptions.AddRange(
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = shawarmaSizeGroup.Id, Name = "Regular", PriceModifierUsd = 0m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = shawarmaSizeGroup.Id, Name = "Large", PriceModifierUsd = 2.00m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = shawarmaSpiceGroup.Id, Name = "Mild", PriceModifierUsd = 0m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = shawarmaSpiceGroup.Id, Name = "Medium", PriceModifierUsd = 0m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = shawarmaSpiceGroup.Id, Name = "Hot", PriceModifierUsd = 0m }
        );

        // ======== Dragon Palace Menu ========
        var ccat1 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r5.Id, Name = "Appetizers", DisplayOrder = 1 };
        var ccat2 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r5.Id, Name = "Main Course", DisplayOrder = 2 };
        var ccat3 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r5.Id, Name = "Beverages", DisplayOrder = 3 };
        db.MenuCategories.AddRange(ccat1, ccat2, ccat3);

        var springRolls = new MenuItem { Id = Guid.NewGuid(), CategoryId = ccat1.Id, RestaurantId = r5.Id, Name = "Spring Rolls", Description = "Crispy vegetable spring rolls.", PriceUsd = 3.50m, StockCount = null };
        var dumplings = new MenuItem { Id = Guid.NewGuid(), CategoryId = ccat1.Id, RestaurantId = r5.Id, Name = "Pork Dumplings", Description = "Steamed or pan-fried dumplings.", PriceUsd = 5.00m, StockCount = 20 };
        var wontonSoup = new MenuItem { Id = Guid.NewGuid(), CategoryId = ccat1.Id, RestaurantId = r5.Id, Name = "Wonton Soup", Description = "Wontons in clear chicken broth.", PriceUsd = 4.00m, StockCount = null };
        var kungPao = new MenuItem { Id = Guid.NewGuid(), CategoryId = ccat2.Id, RestaurantId = r5.Id, Name = "Kung Pao Chicken", Description = "Spicy stir-fried chicken with peanuts.", PriceUsd = 8.50m, StockCount = null };
        var sweetSour = new MenuItem { Id = Guid.NewGuid(), CategoryId = ccat2.Id, RestaurantId = r5.Id, Name = "Sweet & Sour Pork", Description = "Crispy pork in tangy sauce.", PriceUsd = 7.50m, StockCount = null };
        var friedRice = new MenuItem { Id = Guid.NewGuid(), CategoryId = ccat2.Id, RestaurantId = r5.Id, Name = "Fried Rice", Description = "Egg fried rice with vegetables.", PriceUsd = 6.00m, StockCount = null };
        var chowMein = new MenuItem { Id = Guid.NewGuid(), CategoryId = ccat2.Id, RestaurantId = r5.Id, Name = "Chow Mein", Description = "Stir-fried noodles with vegetables.", PriceUsd = 7.00m, StockCount = null };
        var jasmineTea = new MenuItem { Id = Guid.NewGuid(), CategoryId = ccat3.Id, RestaurantId = r5.Id, Name = "Jasmine Tea", Description = "Aromatic jasmine green tea.", PriceUsd = 2.00m, StockCount = null };
        var lycheeJuice = new MenuItem { Id = Guid.NewGuid(), CategoryId = ccat3.Id, RestaurantId = r5.Id, Name = "Lychee Juice", Description = "Chilled lychee beverage.", PriceUsd = 3.00m, StockCount = null };
        db.MenuItems.AddRange(springRolls, dumplings, wontonSoup, kungPao, sweetSour, friedRice, chowMein, jasmineTea, lycheeJuice);

        // Customizations: Kung Pao spice level
        var kungPaoSpice = new CustomizationGroup { Id = Guid.NewGuid(), MenuItemId = kungPao.Id, Name = "Spice Level", IsRequired = true, MaxSelections = 1 };
        db.CustomizationGroups.Add(kungPaoSpice);
        db.CustomizationOptions.AddRange(
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = kungPaoSpice.Id, Name = "Mild", PriceModifierUsd = 0m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = kungPaoSpice.Id, Name = "Medium", PriceModifierUsd = 0m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = kungPaoSpice.Id, Name = "Hot", PriceModifierUsd = 0m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = kungPaoSpice.Id, Name = "Extra Hot", PriceModifierUsd = 0m }
        );

        // ======== Cantina Del Sol Menu ========
        var mcat1 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r6.Id, Name = "Tacos", DisplayOrder = 1 };
        var mcat2 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r6.Id, Name = "Burritos", DisplayOrder = 2 };
        var mcat3 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r6.Id, Name = "Sides", DisplayOrder = 3 };
        db.MenuCategories.AddRange(mcat1, mcat2, mcat3);

        var chickenTacos = new MenuItem { Id = Guid.NewGuid(), CategoryId = mcat1.Id, RestaurantId = r6.Id, Name = "Chicken Tacos", Description = "Three soft tacos with grilled chicken.", PriceUsd = 4.50m, StockCount = null };
        var beefTacos = new MenuItem { Id = Guid.NewGuid(), CategoryId = mcat1.Id, RestaurantId = r6.Id, Name = "Beef Tacos", Description = "Three crispy tacos with seasoned beef.", PriceUsd = 5.00m, StockCount = null };
        var fishTacos = new MenuItem { Id = Guid.NewGuid(), CategoryId = mcat1.Id, RestaurantId = r6.Id, Name = "Fish Tacos", Description = "Battered fish with cabbage slaw.", PriceUsd = 5.50m, StockCount = null };
        var classicBurrito = new MenuItem { Id = Guid.NewGuid(), CategoryId = mcat2.Id, RestaurantId = r6.Id, Name = "Classic Burrito", Description = "Flour tortilla with rice, beans, and meat.", PriceUsd = 7.00m, StockCount = null };
        var supremeBurrito = new MenuItem { Id = Guid.NewGuid(), CategoryId = mcat2.Id, RestaurantId = r6.Id, Name = "Supreme Burrito", Description = "Loaded burrito with guacamole and sour cream.", PriceUsd = 8.50m, StockCount = 8 };
        var guacamole = new MenuItem { Id = Guid.NewGuid(), CategoryId = mcat3.Id, RestaurantId = r6.Id, Name = "Guacamole & Chips", Description = "Fresh avocado dip with tortilla chips.", PriceUsd = 3.00m, StockCount = null };
        var nachos = new MenuItem { Id = Guid.NewGuid(), CategoryId = mcat3.Id, RestaurantId = r6.Id, Name = "Loaded Nachos", Description = "Chips with cheese, beans, and jalapeños.", PriceUsd = 4.00m, StockCount = null };
        var churros = new MenuItem { Id = Guid.NewGuid(), CategoryId = mcat3.Id, RestaurantId = r6.Id, Name = "Churros", Description = "Fried dough with cinnamon and chocolate.", PriceUsd = 3.50m, StockCount = null };
        db.MenuItems.AddRange(chickenTacos, beefTacos, fishTacos, classicBurrito, supremeBurrito, guacamole, nachos, churros);

        // Customizations: Burritos - size + toppings
        var burritoSize = new CustomizationGroup { Id = Guid.NewGuid(), MenuItemId = classicBurrito.Id, Name = "Size", IsRequired = true, MaxSelections = 1 };
        var burritoToppings = new CustomizationGroup { Id = Guid.NewGuid(), MenuItemId = classicBurrito.Id, Name = "Toppings", IsRequired = false, MaxSelections = 3 };
        db.CustomizationGroups.AddRange(burritoSize, burritoToppings);
        db.CustomizationOptions.AddRange(
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = burritoSize.Id, Name = "Regular", PriceModifierUsd = 0m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = burritoSize.Id, Name = "Large", PriceModifierUsd = 2.00m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = burritoToppings.Id, Name = "Guacamole", PriceModifierUsd = 1.50m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = burritoToppings.Id, Name = "Sour Cream", PriceModifierUsd = 0.50m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = burritoToppings.Id, Name = "Extra Cheese", PriceModifierUsd = 1.00m }
        );

        // ======== Taj Mahal Palace Menu ========
        var icat1 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r7.Id, Name = "Starters", DisplayOrder = 1 };
        var icat2 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r7.Id, Name = "Curries", DisplayOrder = 2 };
        var icat3 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r7.Id, Name = "Breads", DisplayOrder = 3 };
        var icat4 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r7.Id, Name = "Beverages", DisplayOrder = 4 };
        db.MenuCategories.AddRange(icat1, icat2, icat3, icat4);

        var samosa = new MenuItem { Id = Guid.NewGuid(), CategoryId = icat1.Id, RestaurantId = r7.Id, Name = "Samosas", Description = "Crispy pastry filled with spiced potatoes.", PriceUsd = 4.00m, StockCount = null };
        var onionBhaji = new MenuItem { Id = Guid.NewGuid(), CategoryId = icat1.Id, RestaurantId = r7.Id, Name = "Onion Bhaji", Description = "Deep-fried onion fritters.", PriceUsd = 3.50m, StockCount = null };
        var pakora = new MenuItem { Id = Guid.NewGuid(), CategoryId = icat1.Id, RestaurantId = r7.Id, Name = "Mixed Pakora", Description = "Assorted vegetable fritters.", PriceUsd = 4.50m, StockCount = 12 };
        var butterChicken = new MenuItem { Id = Guid.NewGuid(), CategoryId = icat2.Id, RestaurantId = r7.Id, Name = "Butter Chicken", Description = "Creamy tomato-based chicken curry.", PriceUsd = 9.00m, StockCount = null };
        var vindaloo = new MenuItem { Id = Guid.NewGuid(), CategoryId = icat2.Id, RestaurantId = r7.Id, Name = "Lamb Vindaloo", Description = "Spicy Goan lamb curry.", PriceUsd = 10.00m, StockCount = null };
        var palakPaneer = new MenuItem { Id = Guid.NewGuid(), CategoryId = icat2.Id, RestaurantId = r7.Id, Name = "Palak Paneer", Description = "Cottage cheese in spinach gravy.", PriceUsd = 8.00m, StockCount = null };
        var garlicNaan = new MenuItem { Id = Guid.NewGuid(), CategoryId = icat3.Id, RestaurantId = r7.Id, Name = "Garlic Naan", Description = "Tandoor-baked bread with garlic.", PriceUsd = 2.50m, StockCount = null };
        var roti = new MenuItem { Id = Guid.NewGuid(), CategoryId = icat3.Id, RestaurantId = r7.Id, Name = "Roti", Description = "Whole wheat flatbread.", PriceUsd = 2.00m, StockCount = null };
        var mangoLassi = new MenuItem { Id = Guid.NewGuid(), CategoryId = icat4.Id, RestaurantId = r7.Id, Name = "Mango Lassi", Description = "Yogurt drink with mango pulp.", PriceUsd = 3.50m, StockCount = null };
        var chai = new MenuItem { Id = Guid.NewGuid(), CategoryId = icat4.Id, RestaurantId = r7.Id, Name = "Masala Chai", Description = "Spiced Indian tea.", PriceUsd = 2.00m, StockCount = null };
        db.MenuItems.AddRange(samosa, onionBhaji, pakora, butterChicken, vindaloo, palakPaneer, garlicNaan, roti, mangoLassi, chai);

        // Customizations: Curry spice level
        var currySpice = new CustomizationGroup { Id = Guid.NewGuid(), MenuItemId = butterChicken.Id, Name = "Spice Level", IsRequired = true, MaxSelections = 1 };
        db.CustomizationGroups.Add(currySpice);
        db.CustomizationOptions.AddRange(
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = currySpice.Id, Name = "Mild", PriceModifierUsd = 0m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = currySpice.Id, Name = "Medium", PriceModifierUsd = 0m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = currySpice.Id, Name = "Hot", PriceModifierUsd = 0m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = currySpice.Id, Name = "Indian Hot", PriceModifierUsd = 0m }
        );

        // ======== Le Petit Bistro Menu ========
        var fcat1 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r8.Id, Name = "Appetizers", DisplayOrder = 1 };
        var fcat2 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r8.Id, Name = "Main Course", DisplayOrder = 2 };
        var fcat3 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r8.Id, Name = "Desserts", DisplayOrder = 3 };
        db.MenuCategories.AddRange(fcat1, fcat2, fcat3);

        var onionSoup = new MenuItem { Id = Guid.NewGuid(), CategoryId = fcat1.Id, RestaurantId = r8.Id, Name = "French Onion Soup", Description = "Caramelized onion with Gruyère crouton.", PriceUsd = 8.00m, StockCount = null };
        var escargots = new MenuItem { Id = Guid.NewGuid(), CategoryId = fcat1.Id, RestaurantId = r8.Id, Name = "Escargots de Bourgogne", Description = "Snails in garlic herb butter.", PriceUsd = 12.00m, StockCount = null };
        var foieGras = new MenuItem { Id = Guid.NewGuid(), CategoryId = fcat1.Id, RestaurantId = r8.Id, Name = "Foie Gras", Description = "Seared foie gras with fig compote.", PriceUsd = 18.00m, StockCount = 5 };
        var coqAuVin = new MenuItem { Id = Guid.NewGuid(), CategoryId = fcat2.Id, RestaurantId = r8.Id, Name = "Coq au Vin", Description = "Braised chicken in red wine.", PriceUsd = 16.00m, StockCount = null };
        var beefBourguignon = new MenuItem { Id = Guid.NewGuid(), CategoryId = fcat2.Id, RestaurantId = r8.Id, Name = "Beef Bourguignon", Description = "Slow-cooked beef in Burgundy wine.", PriceUsd = 18.00m, StockCount = null };
        var duckConfit = new MenuItem { Id = Guid.NewGuid(), CategoryId = fcat2.Id, RestaurantId = r8.Id, Name = "Duck Confit", Description = "Crispy duck leg with roasted potatoes.", PriceUsd = 20.00m, StockCount = null };
        var cremeBrulee = new MenuItem { Id = Guid.NewGuid(), CategoryId = fcat3.Id, RestaurantId = r8.Id, Name = "Crème Brûlée", Description = "Vanilla custard with caramelized sugar.", PriceUsd = 7.00m, StockCount = null };
        var chocoMousse = new MenuItem { Id = Guid.NewGuid(), CategoryId = fcat3.Id, RestaurantId = r8.Id, Name = "Chocolate Mousse", Description = "Rich dark chocolate mousse.", PriceUsd = 8.00m, StockCount = null };
        var macarons = new MenuItem { Id = Guid.NewGuid(), CategoryId = fcat3.Id, RestaurantId = r8.Id, Name = "Macarons", Description = "Assorted French macarons (6 pcs).", PriceUsd = 5.00m, StockCount = null };
        db.MenuItems.AddRange(onionSoup, escargots, foieGras, coqAuVin, beefBourguignon, duckConfit, cremeBrulee, chocoMousse, macarons);

        // Customizations: Doneness for steaks
        var donenessGroup = new CustomizationGroup { Id = Guid.NewGuid(), MenuItemId = duckConfit.Id, Name = "Doneness", IsRequired = true, MaxSelections = 1 };
        db.CustomizationGroups.Add(donenessGroup);
        db.CustomizationOptions.AddRange(
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = donenessGroup.Id, Name = "Rare", PriceModifierUsd = 0m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = donenessGroup.Id, Name = "Medium Rare", PriceModifierUsd = 0m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = donenessGroup.Id, Name = "Medium", PriceModifierUsd = 0m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = donenessGroup.Id, Name = "Well Done", PriceModifierUsd = 0m }
        );

        // ======== The Greek Taverna Menu ========
        var gcat1 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r9.Id, Name = "Appetizers", DisplayOrder = 1 };
        var gcat2 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r9.Id, Name = "Mains", DisplayOrder = 2 };
        var gcat3 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r9.Id, Name = "Salads", DisplayOrder = 3 };
        db.MenuCategories.AddRange(gcat1, gcat2, gcat3);

        var tzatziki = new MenuItem { Id = Guid.NewGuid(), CategoryId = gcat1.Id, RestaurantId = r9.Id, Name = "Tzatziki", Description = "Cucumber yogurt dip with pita.", PriceUsd = 4.00m, StockCount = null };
        var spanakopita = new MenuItem { Id = Guid.NewGuid(), CategoryId = gcat1.Id, RestaurantId = r9.Id, Name = "Spanakopita", Description = "Spinach and feta pastry triangles.", PriceUsd = 5.00m, StockCount = null };
        var dolmades = new MenuItem { Id = Guid.NewGuid(), CategoryId = gcat1.Id, RestaurantId = r9.Id, Name = "Dolmades", Description = "Rice-stuffed vine leaves with avgolemono.", PriceUsd = 4.50m, StockCount = 18 };
        var moussaka = new MenuItem { Id = Guid.NewGuid(), CategoryId = gcat2.Id, RestaurantId = r9.Id, Name = "Moussaka", Description = "Layered eggplant and minced beef bake.", PriceUsd = 10.00m, StockCount = null };
        var souvlaki = new MenuItem { Id = Guid.NewGuid(), CategoryId = gcat2.Id, RestaurantId = r9.Id, Name = "Souvlaki", Description = "Grilled pork skewers with pita.", PriceUsd = 8.00m, StockCount = null };
        var gyrosPlate = new MenuItem { Id = Guid.NewGuid(), CategoryId = gcat2.Id, RestaurantId = r9.Id, Name = "Gyros Plate", Description = "Rotisserie meat with rice and salad.", PriceUsd = 9.00m, StockCount = null };
        var greekSalad = new MenuItem { Id = Guid.NewGuid(), CategoryId = gcat3.Id, RestaurantId = r9.Id, Name = "Greek Salad", Description = "Feta, olives, tomato, cucumber.", PriceUsd = 6.00m, StockCount = null };
        var horiatiki = new MenuItem { Id = Guid.NewGuid(), CategoryId = gcat3.Id, RestaurantId = r9.Id, Name = "Horiatiki", Description = "Traditional village salad with capers.", PriceUsd = 7.00m, StockCount = null };
        db.MenuItems.AddRange(tzatziki, spanakopita, dolmades, moussaka, souvlaki, gyrosPlate, greekSalad, horiatiki);

        // Customizations: Gyros - meat choice
        var gyrosMeat = new CustomizationGroup { Id = Guid.NewGuid(), MenuItemId = gyrosPlate.Id, Name = "Meat Choice", IsRequired = true, MaxSelections = 1 };
        var gyrosExtras = new CustomizationGroup { Id = Guid.NewGuid(), MenuItemId = gyrosPlate.Id, Name = "Extras", IsRequired = false, MaxSelections = 2 };
        db.CustomizationGroups.AddRange(gyrosMeat, gyrosExtras);
        db.CustomizationOptions.AddRange(
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = gyrosMeat.Id, Name = "Chicken", PriceModifierUsd = 0m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = gyrosMeat.Id, Name = "Lamb", PriceModifierUsd = 0m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = gyrosMeat.Id, Name = "Mixed", PriceModifierUsd = 1.00m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = gyrosExtras.Id, Name = "Add Feta", PriceModifierUsd = 1.00m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = gyrosExtras.Id, Name = "Extra Tzatziki", PriceModifierUsd = 0.50m }
        );

        // ======== Seoul Kitchen Menu ========
        var kcat1 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r10.Id, Name = "Appetizers", DisplayOrder = 1 };
        var kcat2 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r10.Id, Name = "BBQ", DisplayOrder = 2 };
        var kcat3 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r10.Id, Name = "Bowls", DisplayOrder = 3 };
        var kcat4 = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = r10.Id, Name = "Sides", DisplayOrder = 4 };
        db.MenuCategories.AddRange(kcat1, kcat2, kcat3, kcat4);

        var kimchiPancake = new MenuItem { Id = Guid.NewGuid(), CategoryId = kcat1.Id, RestaurantId = r10.Id, Name = "Kimchi Pancake", Description = "Savory pancake with fermented cabbage.", PriceUsd = 5.00m, StockCount = null };
        var mandu = new MenuItem { Id = Guid.NewGuid(), CategoryId = kcat1.Id, RestaurantId = r10.Id, Name = "Mandu", Description = "Korean dumplings steamed or fried.", PriceUsd = 6.00m, StockCount = 15 };
        var tteokbokki = new MenuItem { Id = Guid.NewGuid(), CategoryId = kcat1.Id, RestaurantId = r10.Id, Name = "Tteokbokki", Description = "Spicy stir-fried rice cakes.", PriceUsd = 5.50m, StockCount = null };
        var bulgogi = new MenuItem { Id = Guid.NewGuid(), CategoryId = kcat2.Id, RestaurantId = r10.Id, Name = "Bulgogi", Description = "Marinated grilled beef.", PriceUsd = 11.00m, StockCount = null };
        var galbi = new MenuItem { Id = Guid.NewGuid(), CategoryId = kcat2.Id, RestaurantId = r10.Id, Name = "Galbi", Description = "Grilled beef short ribs.", PriceUsd = 14.00m, StockCount = null };
        var porkBelly = new MenuItem { Id = Guid.NewGuid(), CategoryId = kcat2.Id, RestaurantId = r10.Id, Name = "Pork Belly", Description = "Thick-cut grilled pork belly.", PriceUsd = 12.00m, StockCount = null };
        var bibimbap = new MenuItem { Id = Guid.NewGuid(), CategoryId = kcat3.Id, RestaurantId = r10.Id, Name = "Bibimbap", Description = "Mixed rice bowl with vegetables and egg.", PriceUsd = 9.00m, StockCount = null };
        var japchae = new MenuItem { Id = Guid.NewGuid(), CategoryId = kcat3.Id, RestaurantId = r10.Id, Name = "Japchae", Description = "Stir-fried glass noodles with vegetables.", PriceUsd = 8.50m, StockCount = null };
        var kimchiSide = new MenuItem { Id = Guid.NewGuid(), CategoryId = kcat4.Id, RestaurantId = r10.Id, Name = "Kimchi", Description = "Traditional fermented cabbage side.", PriceUsd = 2.00m, StockCount = null };
        var pickledRadish = new MenuItem { Id = Guid.NewGuid(), CategoryId = kcat4.Id, RestaurantId = r10.Id, Name = "Pickled Radish", Description = "Sweet and tangy pickled radish.", PriceUsd = 1.50m, StockCount = null };
        db.MenuItems.AddRange(kimchiPancake, mandu, tteokbokki, bulgogi, galbi, porkBelly, bibimbap, japchae, kimchiSide, pickledRadish);

        // Customizations: Bibimbap - spice + add egg
        var bibimbapSpice = new CustomizationGroup { Id = Guid.NewGuid(), MenuItemId = bibimbap.Id, Name = "Spice Level", IsRequired = true, MaxSelections = 1 };
        var bibimbapExtras = new CustomizationGroup { Id = Guid.NewGuid(), MenuItemId = bibimbap.Id, Name = "Extras", IsRequired = false, MaxSelections = 2 };
        db.CustomizationGroups.AddRange(bibimbapSpice, bibimbapExtras);
        db.CustomizationOptions.AddRange(
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = bibimbapSpice.Id, Name = "Mild", PriceModifierUsd = 0m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = bibimbapSpice.Id, Name = "Medium", PriceModifierUsd = 0m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = bibimbapSpice.Id, Name = "Hot", PriceModifierUsd = 0m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = bibimbapExtras.Id, Name = "Add Egg", PriceModifierUsd = 1.00m },
            new CustomizationOption { Id = Guid.NewGuid(), GroupId = bibimbapExtras.Id, Name = "Add Avocado", PriceModifierUsd = 1.50m }
        );

        // ---- Coupons ----
        var now = DateTime.UtcNow;
        db.Coupons.AddRange(
            new Coupon
            {
                Id = Guid.NewGuid(), Code = "SUMMER10", DiscountType = "percent",
                DiscountValue = 10m, MinSubtotalUsd = 10m, MaxUses = 100,
                UsesCount = 0, ValidFrom = now.AddDays(-30), ValidUntil = now.AddDays(60), Active = true
            },
            new Coupon
            {
                Id = Guid.NewGuid(), Code = "FREESHIP", DiscountType = "fixed",
                DiscountValue = 1.50m, MinSubtotalUsd = 0m, MaxUses = null,
                UsesCount = 0, ValidFrom = now.AddDays(-30), ValidUntil = now.AddDays(30), Active = true
            },
            new Coupon
            {
                Id = Guid.NewGuid(), Code = "EXPIRED50", DiscountType = "percent",
                DiscountValue = 50.00m, MinSubtotalUsd = 0.00m, MaxUses = 10,
                UsesCount = 0, Active = false,
                ValidFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                ValidUntil = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc)
            },
            new Coupon
            {
                Id = Guid.NewGuid(), Code = "BIGSPENDER", DiscountType = "percent",
                DiscountValue = 20.00m, MinSubtotalUsd = 100.00m, MaxUses = 5,
                UsesCount = 0, Active = true,
                ValidFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                ValidUntil = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc)
            }
        );

        await db.SaveChangesAsync();
    }
}
