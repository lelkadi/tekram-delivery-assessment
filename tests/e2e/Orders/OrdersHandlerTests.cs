namespace Tekram.E2E.Orders;

using FluentAssertions;

/// <summary>
/// Structural verification for issue #16 (Part 2 Slice 3.2 — Orders).
/// Verifies the orders endpoint is wired and handler covers all 12 steps.
/// Full HTTP integration tests live in tests/Tekram.Tests/orders/OrderIntegrationTests.cs.
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

    // ── Endpoint wiring verification ────────────────────────────────────

    [Fact]
    public void AC_Endpoint_IsWiredInProgramCs()
    {
        var content = ReadAll("src/Tekram.Api/Program.cs");

        content.Should().Contain("using Tekram.Api.src.orders.Presentation;");
        content.Should().NotContain("// using Tekram.Api.src.orders.Presentation;");
        content.Should().Contain("app.MapOrderEndpoints();");
        content.Should().NotContain("// app.MapOrderEndpoints();");
    }

    [Fact]
    public void AC_DI_IsRegisteredInServiceCollectionExtensions()
    {
        var content = ReadAll("src/Tekram.Api/src/shared/ServiceCollectionExtensions.cs");

        content.Should().Contain("services.AddScoped<IOrderRepository, OrderRepository>");
        content.Should().Contain("services.AddScoped<ICouponRepository, CouponRepository>");
        content.Should().Contain("services.AddScoped<IMenuPricingReader, MenuPricingReader>");
        content.Should().Contain("services.AddScoped<orders.Application.Handlers.PlaceOrderHandler>");

        // Must not be commented out
        content.Should().NotContain("// services.AddScoped<IOrderRepository");
    }

    // ── Handler implementation verification ─────────────────────────────

    [Fact]
    public void AC_Handler_HasAllRequiredSteps()
    {
        var content = ReadAll("src/Tekram.Api/src/orders/Application/Handlers/PlaceOrderHandler.cs");

        // Verification gate
        content.Should().Contain("EmailVerified");
        content.Should().Contain("PhoneVerified");
        content.Should().Contain("VerificationRequired");
        // Price parity
        content.Should().Contain("GetItemForPricingAsync");
        // Stock check
        content.Should().Contain("StockCount");
        // Customization pricing + validation
        content.Should().Contain("CustomizationChoices");
        content.Should().Contain("PriceModifierUsd");
        content.Should().Contain("validGroupIds");
        content.Should().Contain("option.GroupId");
        // Coupon (atomic: uses UsesCount not IncrementUsageAsync)
        content.Should().Contain("GetByCodeAsync");
        content.Should().Contain("ApplyCoupon");
        content.Should().Contain("UsesCount");
        // Pricing policies
        content.Should().Contain("CalculateSmallOrderSurcharge");
        content.Should().Contain("CalculateDeliveryFee");
        content.Should().Contain("CalculateTotal");
        // Persist + response
        content.Should().Contain("AddAsync");
        content.Should().Contain("OrderResponse");
    }

    // ── Infrastructure verification ─────────────────────────────────────

    [Fact]
    public void AC_Infrastructure_FilesExist()
    {
        ReadAll("src/Tekram.Api/src/orders/Infrastructure/OrderRepository.cs")
            .Should().Contain("class OrderRepository");
        ReadAll("src/Tekram.Api/src/orders/Infrastructure/CouponRepository.cs")
            .Should().Contain("class CouponRepository");
        ReadAll("src/Tekram.Api/src/orders/Infrastructure/MenuPricingReader.cs")
            .Should().Contain("class MenuPricingReader");
        ReadAll("src/Tekram.Api/src/orders/Presentation/OrderEndpoints.cs")
            .Should().Contain("MapGroup(\"/api/food/orders\")");
    }
}
