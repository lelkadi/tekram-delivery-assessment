namespace Tekram.E2E.Restaurants;

using FluentAssertions;

/// <summary>
/// Black-box structural verification for issue #12 (Part 2 Slice 2.1 — Restaurants:
/// domain entities, DTOs, validators, interfaces). This slice ships zero HTTP
/// endpoints of its own (endpoints land in #13), so the tests below verify file
/// existence, namespace correctness, expected property/method presence, and
/// FluentValidation rule conformance — the same static-analysis contract the
/// Architect Spec and PM verification gate on.
/// </summary>
[Trait("issue", "12")]
public class RestaurantDomainTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    private static readonly string SrcRoot = Path.Combine(RepoRoot,
        "src", "Tekram.Api", "src", "restaurants");

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Tekram.sln")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName
            ?? throw new InvalidOperationException("Could not find repository root (Tekram.sln not found)");
    }

    private static string ReadAll(string relativePath)
    {
        var full = Path.Combine(RepoRoot, relativePath);
        File.Exists(full).Should().BeTrue($"file must exist: {relativePath}");
        return File.ReadAllText(full);
    }

    // ── AC1: 5 Domain entities (blueprint SS5.1.1–SS5.1.5) ──────────────

    [Fact]
    public void AC1_RestaurantEntityExistsWithAllFields()
    {
        var content = ReadAll("src/Tekram.Api/src/restaurants/Domain/Restaurant.cs");

        content.Should().Contain("namespace Tekram.Api.src.restaurants.Domain");
        content.Should().Contain("class Restaurant");
        content.Should().Contain("Guid Id");
        content.Should().Contain("string Name");
        content.Should().Contain("string? Description");
        content.Should().Contain("string Cuisine");
        content.Should().Contain("decimal Rating");
        content.Should().Contain("int PriceTier");
        content.Should().Contain("int AvgPrepMinutes");
        content.Should().Contain("string Status");
        content.Should().Contain("decimal Latitude");
        content.Should().Contain("decimal Longitude");
        content.Should().Contain("DateTime CreatedAt");
        content.Should().Contain("DateTime UpdatedAt");
        content.Should().Contain("DateTime? DeletedAt");
    }

    [Fact]
    public void AC1_MenuCategoryEntityExistsWithAllFields()
    {
        var content = ReadAll("src/Tekram.Api/src/restaurants/Domain/MenuCategory.cs");

        content.Should().Contain("namespace Tekram.Api.src.restaurants.Domain");
        content.Should().Contain("class MenuCategory");
        content.Should().Contain("Guid Id");
        content.Should().Contain("Guid RestaurantId");
        content.Should().Contain("string Name");
        content.Should().Contain("int DisplayOrder");
    }

    [Fact]
    public void AC1_MenuItemEntityExistsWithAllFields()
    {
        var content = ReadAll("src/Tekram.Api/src/restaurants/Domain/MenuItem.cs");

        content.Should().Contain("namespace Tekram.Api.src.restaurants.Domain");
        content.Should().Contain("class MenuItem");
        content.Should().Contain("Guid Id");
        content.Should().Contain("Guid CategoryId");
        content.Should().Contain("Guid RestaurantId");
        content.Should().Contain("string Name");
        content.Should().Contain("string? Description");
        content.Should().Contain("decimal PriceUsd");
        content.Should().Contain("int? StockCount");
        content.Should().Contain("DateTime CreatedAt");
        content.Should().Contain("DateTime UpdatedAt");
        content.Should().Contain("DateTime? DeletedAt");
    }

    [Fact]
    public void AC1_CustomizationGroupEntityExistsWithAllFields()
    {
        var content = ReadAll("src/Tekram.Api/src/restaurants/Domain/CustomizationGroup.cs");

        content.Should().Contain("namespace Tekram.Api.src.restaurants.Domain");
        content.Should().Contain("class CustomizationGroup");
        content.Should().Contain("Guid Id");
        content.Should().Contain("Guid MenuItemId");
        content.Should().Contain("string Name");
        content.Should().Contain("bool IsRequired");
        content.Should().Contain("int MaxSelections");
    }

    [Fact]
    public void AC1_CustomizationOptionEntityExistsWithAllFields()
    {
        var content = ReadAll("src/Tekram.Api/src/restaurants/Domain/CustomizationOption.cs");

        content.Should().Contain("namespace Tekram.Api.src.restaurants.Domain");
        content.Should().Contain("class CustomizationOption");
        content.Should().Contain("Guid Id");
        content.Should().Contain("Guid GroupId");
        content.Should().Contain("string Name");
        content.Should().Contain("decimal PriceModifierUsd");
    }

    // ── AC2: 3 DTO records (blueprint SS5.2.1–SS5.2.3) ──────────────────

    [Fact]
    public void AC2_SearchRestaurantsRequestDtoExistsWithCorrectFields()
    {
        var content = ReadAll("src/Tekram.Api/src/restaurants/Application/DTOs/SearchRestaurantsRequest.cs");

        content.Should().Contain("namespace Tekram.Api.src.restaurants.Application.DTOs");
        content.Should().Contain("record SearchRestaurantsRequest");
        content.Should().Contain("string? Search");
        content.Should().Contain("string? Cuisine");
        content.Should().Contain("int? PriceTier");
        content.Should().Contain("int Page");
        content.Should().Contain("int Limit");
    }

    [Fact]
    public void AC2_RestaurantListResponseDtoExistsWithCorrectStructure()
    {
        var content = ReadAll("src/Tekram.Api/src/restaurants/Application/DTOs/RestaurantListResponse.cs");

        content.Should().Contain("namespace Tekram.Api.src.restaurants.Application.DTOs");
        content.Should().Contain("record RestaurantListItem");
        content.Should().Contain("record RestaurantListResponse");
        // Must include pagination envelope
        content.Should().Contain("Pagination");
    }

    [Fact]
    public void AC2_MenuResponseDtoExistsWithNestedRecords()
    {
        var content = ReadAll("src/Tekram.Api/src/restaurants/Application/DTOs/MenuResponse.cs");

        content.Should().Contain("namespace Tekram.Api.src.restaurants.Application.DTOs");
        content.Should().Contain("record MenuOptionResponse");
        content.Should().Contain("record MenuCustomizationGroupResponse");
        content.Should().Contain("record MenuItemResponse");
        content.Should().Contain("record MenuCategoryResponse");
        content.Should().Contain("record MenuResponse");
    }

    // ── AC3: SearchRestaurantsRequestValidator (blueprint SS5.3.1) ──────

    [Fact]
    public void AC3_ValidatorExistsWithAllFluentValidationRules()
    {
        var content = ReadAll("src/Tekram.Api/src/restaurants/Application/Validators/SearchRestaurantsRequestValidator.cs");

        content.Should().Contain("namespace Tekram.Api.src.restaurants.Application.Validators");
        content.Should().Contain("class SearchRestaurantsRequestValidator");
        content.Should().Contain("AbstractValidator<SearchRestaurantsRequest>");

        // Blueprint SS5.3.1 rules (PM-verified)
        content.Should().Contain("RuleFor(x => x.Page).GreaterThan(0)");
        content.Should().Contain("RuleFor(x => x.Limit).InclusiveBetween(1, 50)");
        content.Should().Contain("RuleFor(x => x.PriceTier).InclusiveBetween(1, 4)");
        content.Should().Contain("RuleFor(x => x.Search).MaximumLength(200)");
        content.Should().Contain("RuleFor(x => x.Cuisine).MaximumLength(100)");
    }

    // ── AC4: Repository interfaces (blueprint SS5.4.1–SS5.4.2) ──────────

    [Fact]
    public void AC4_IRestaurantRepositoryHasCorrectMethodSignatures()
    {
        var content = ReadAll("src/Tekram.Api/src/restaurants/Application/Interfaces/IRestaurantRepository.cs");

        content.Should().Contain("namespace Tekram.Api.src.restaurants.Application.Interfaces");
        content.Should().Contain("interface IRestaurantRepository");
        content.Should().Contain("SearchAsync");
        content.Should().Contain("GetByIdAsync");
    }

    [Fact]
    public void AC4_IMenuRepositoryHasAllSixQueryMethods()
    {
        var content = ReadAll("src/Tekram.Api/src/restaurants/Application/Interfaces/IMenuRepository.cs");

        content.Should().Contain("namespace Tekram.Api.src.restaurants.Application.Interfaces");
        content.Should().Contain("interface IMenuRepository");

        // 6 query methods per blueprint SS5.4.2 (PM-verified)
        content.Should().Contain("GetCategoriesByRestaurantAsync");
        content.Should().Contain("GetItemsByCategoryAsync");
        content.Should().Contain("GetItemByIdAsync");
        content.Should().Contain("GetCustomizationGroupsByItemAsync");
        content.Should().Contain("GetOptionsByGroupAsync");
        content.Should().Contain("GetItemsByRestaurantAsync");
    }

    // ── Cross-cutting spec compliance ───────────────────────────────────

    [Fact]
    public void AllRestaurantSourceFilesUseCorrectModuleNamespace()
    {
        // Every restaurant file must live under Tekram.Api.src.restaurants
        var files = Directory.GetFiles(SrcRoot, "*.cs", SearchOption.AllDirectories);

        files.Should().NotBeEmpty("at least one restaurant source file must exist");

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            if (string.IsNullOrWhiteSpace(content)) continue; // placeholder — not this slice's scope
            content.Should().Contain("namespace Tekram.Api.src.restaurants",
                $"file '{Path.GetFileName(file)}' must use the module namespace per architecture §3 (schema-per-module)");
        }
    }
}
