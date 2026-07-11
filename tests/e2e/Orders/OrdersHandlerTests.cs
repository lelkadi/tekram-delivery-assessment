namespace Tekram.E2E.Orders;

using FluentAssertions;

/// <summary>
/// Structural verification for issue #16 (Part 2 Slice 3.2 — Orders:
/// PlaceOrderHandler, infrastructure repositories, presentation endpoint).
/// Verifies file existence and key implementation details against blueprint §§6.5–6.7.
/// </summary>
[Trait("issue", "16")]
public class OrdersHandlerTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Tekram.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Repo root not found");
    }

    private static string ReadAll(string path)
    {
        var full = Path.Combine(RepoRoot, path);
        File.Exists(full).Should().BeTrue($"file must exist: {path}");
        return File.ReadAllText(full);
    }

    // ── AC1: PlaceOrderHandler (SS6.5.1) — all 12 steps ──────────────

    [Fact]
    public void AC1_Handler_HasAllTwelveSteps()
    {
        var content = ReadAll("src/Tekram.Api/src/orders/Application/Handlers/PlaceOrderHandler.cs");

        // Step 1: validation
        content.Should().Contain("ValidateAndThrowAsync");
        // Step 2: verification gate
        content.Should().Contain("EmailVerified");
        content.Should().Contain("PhoneVerified");
        content.Should().Contain("VerificationRequired");
        // Step 3: price parity
        content.Should().Contain("GetItemForPricingAsync");
        // Step 4: stock check
        content.Should().Contain("StockCount");
        content.Should().Contain("ItemUnavailable");
        // Step 5: customization pricing
        content.Should().Contain("CustomizationChoices");
        content.Should().Contain("PriceModifierUsd");
        // Step 6: subtotal
        content.Should().Contain("subtotalUsd");
        content.Should().Contain("effectiveUnitPrice * item.Quantity");
        // Step 7: surcharge
        content.Should().Contain("CalculateSmallOrderSurcharge");
        // Step 8: delivery fee
        content.Should().Contain("CalculateDeliveryFee");
        // Step 9: coupon
        content.Should().Contain("GetByCodeAsync");
        content.Should().Contain("ApplyCoupon");
        content.Should().Contain("UsesCount");
        // Step 10: total
        content.Should().Contain("CalculateTotal");
        // Step 11: persist
        content.Should().Contain("AddAsync");
        // Step 12: response
        content.Should().Contain("new OrderResponse");
        content.Should().Contain("BookingId:");
    }

    // ── AC2: Infrastructure repositories (SS6.6) ──────────────────────

    [Fact]
    public void AC2_OrderRepository_Exists()
    {
        var content = ReadAll("src/Tekram.Api/src/orders/Infrastructure/OrderRepository.cs");
        content.Should().Contain("class OrderRepository");
        content.Should().Contain("IOrderRepository");
        content.Should().Contain("AddAsync");
        content.Should().Contain("SaveChangesAsync");
    }

    [Fact]
    public void AC2_CouponRepository_Exists()
    {
        var content = ReadAll("src/Tekram.Api/src/orders/Infrastructure/CouponRepository.cs");
        content.Should().Contain("class CouponRepository");
        content.Should().Contain("ICouponRepository");
        content.Should().Contain("GetByCodeAsync");
        content.Should().Contain("UsesCount");
    }

    [Fact]
    public void AC2_MenuPricingReader_Exists()
    {
        var content = ReadAll("src/Tekram.Api/src/orders/Infrastructure/MenuPricingReader.cs");
        content.Should().Contain("class MenuPricingReader");
        content.Should().Contain("IMenuPricingReader");
        content.Should().Contain("GetItemForPricingAsync");
        content.Should().Contain("GetCustomizationGroupsAsync");
        content.Should().Contain("GetOptionAsync");
    }

    // ── AC3: OrderEndpoints (SS6.7.1) ────────────────────────────────

    [Fact]
    public void AC3_OrderEndpoints_HasCorrectStructure()
    {
        var content = ReadAll("src/Tekram.Api/src/orders/Presentation/OrderEndpoints.cs");
        content.Should().Contain("MapGroup(\"/api/food/orders\")");
        content.Should().Contain("MapPost");
        content.Should().Contain("RequireAuthorization()");
        content.Should().Contain("Results.Created");
        content.Should().Contain("PlaceOrderHandler");
        content.Should().Contain("ClaimsPrincipal");
    }

    // ── Cross-cutting ──────────────────────────────────────────────────

    [Fact]
    public void AllOrderHandlerFilesUseCorrectNamespace()
    {
        var dirs = new[]
        {
            "src/Tekram.Api/src/orders/Application/Handlers",
            "src/Tekram.Api/src/orders/Infrastructure",
            "src/Tekram.Api/src/orders/Presentation"
        };

        foreach (var dir in dirs)
        {
            var fullDir = Path.Combine(RepoRoot, dir);
            if (!Directory.Exists(fullDir)) continue;
            foreach (var file in Directory.GetFiles(fullDir, "*.cs"))
            {
                var content = File.ReadAllText(file);
                if (string.IsNullOrWhiteSpace(content)) continue;
                content.Should().Contain("namespace Tekram.Api.src.orders",
                    $"file '{Path.GetFileName(file)}' must use orders module namespace");
            }
        }
    }
}
