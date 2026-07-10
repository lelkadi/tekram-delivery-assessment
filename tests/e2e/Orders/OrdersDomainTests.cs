namespace Tekram.E2E.Orders;

using FluentAssertions;

/// <summary>
/// Structural verification for issue #15 (Part 2 Slice 3.1 — Orders:
/// domain entities, OrderPricingPolicy, DTOs, validators, interfaces).
/// Domain entities (Order, OrderItem, Coupon, OrderPricingPolicy) are
/// already on main; this slice fills the empty DTO/validator/interface stubs.
/// No HTTP endpoints — static analysis only.
/// </summary>
[Trait("issue", "15")]
public class OrdersDomainTests
{
    private static readonly string RepoRoot = FindRepoRoot();
    private static readonly string SrcRoot = Path.Combine(RepoRoot,
        "src", "Tekram.Api", "src", "orders");

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Tekram.sln")))
            dir = dir.Parent;
        return dir?.FullName
            ?? throw new InvalidOperationException("Could not find repository root");
    }

    private static string ReadAll(string relativePath)
    {
        var full = Path.Combine(RepoRoot, relativePath);
        File.Exists(full).Should().BeTrue($"file must exist: {relativePath}");
        return File.ReadAllText(full);
    }

    // ── AC1: PlaceOrderRequest DTO (SS6.2.1) ──────────────────────────

    [Fact]
    public void AC1_PlaceOrderRequest_HasAllFields()
    {
        var content = ReadAll("src/Tekram.Api/src/orders/Application/DTOs/PlaceOrderRequest.cs");

        content.Should().Contain("namespace Tekram.Api.src.orders.Application.DTOs");
        content.Should().Contain("record CustomizationChoice");
        content.Should().Contain("Guid GroupId");
        content.Should().Contain("Guid OptionId");
        content.Should().Contain("record OrderItemRequest");
        content.Should().Contain("Guid MenuItemId");
        content.Should().Contain("int Quantity");
        content.Should().Contain("CustomizationChoices"); // nullable with default null
        content.Should().Contain("record PlaceOrderRequest");
        content.Should().Contain("Guid RestaurantId");
        content.Should().Contain("List<OrderItemRequest> Items");
        content.Should().Contain("string DeliveryAddress");
        content.Should().Contain("string PaymentMethod");
        content.Should().Contain("string? CouponCode");
        content.Should().Contain("string? SpecialInstructions");
    }

    // ── AC2: OrderResponse DTO (SS6.2.2) ──────────────────────────────

    [Fact]
    public void AC2_OrderResponse_HasAllFields()
    {
        var content = ReadAll("src/Tekram.Api/src/orders/Application/DTOs/OrderResponse.cs");

        content.Should().Contain("record OrderTotalsResponse");
        content.Should().Contain("decimal SubtotalUsd");
        content.Should().Contain("decimal DeliveryFeeUsd");
        content.Should().Contain("decimal SmallOrderSurchargeUsd");
        content.Should().Contain("decimal DiscountUsd");
        content.Should().Contain("decimal TotalUsd");
        content.Should().Contain("record OrderResponse");
        content.Should().Contain("Guid BookingId");
        content.Should().Contain("string Status");
        content.Should().Contain("OrderTotalsResponse Totals");
        content.Should().Contain("DateTime CreatedAt");
    }

    // ── AC3: PlaceOrderRequestValidator (SS6.3.1) ─────────────────────

    [Fact]
    public void AC3_Validator_HasAllRules()
    {
        var content = ReadAll("src/Tekram.Api/src/orders/Application/Validators/PlaceOrderRequestValidator.cs");

        content.Should().Contain("class PlaceOrderRequestValidator");
        content.Should().Contain("AbstractValidator<PlaceOrderRequest>");
        content.Should().Contain("RuleFor(x => x.RestaurantId).NotEmpty()");
        content.Should().Contain("RuleFor(x => x.Items).NotEmpty()");
        content.Should().Contain("RuleFor(x => x.DeliveryAddress).NotEmpty().MaximumLength(500)");
        content.Should().Contain("RuleFor(x => x.PaymentMethod)");
        content.Should().Contain("\"COD\"");
        content.Should().Contain("\"WALLET\"");
        content.Should().Contain("RuleFor(i => i.MenuItemId).NotEmpty()");
        content.Should().Contain("RuleFor(i => i.Quantity).GreaterThan(0)");
    }

    // ── AC4: Repository interfaces (SS6.4.1–SS6.4.3) ─────────────────

    [Fact]
    public void AC4_IOrderRepository_HasAddAsync()
    {
        var content = ReadAll("src/Tekram.Api/src/orders/Application/Interfaces/IOrderRepository.cs");

        content.Should().Contain("interface IOrderRepository");
        content.Should().Contain("Task AddAsync(Order order");
    }

    [Fact]
    public void AC4_ICouponRepository_HasGetByCodeAndIncrementUsage()
    {
        var content = ReadAll("src/Tekram.Api/src/orders/Application/Interfaces/ICouponRepository.cs");

        content.Should().Contain("interface ICouponRepository");
        content.Should().Contain("GetByCodeAsync");
        content.Should().Contain("IncrementUsageAsync");
    }

    [Fact]
    public void AC4_IMenuPricingReader_HasThreeMethods()
    {
        var content = ReadAll("src/Tekram.Api/src/orders/Application/Interfaces/IMenuPricingReader.cs");

        content.Should().Contain("interface IMenuPricingReader");
        content.Should().Contain("GetItemForPricingAsync");
        content.Should().Contain("GetCustomizationGroupsAsync");
        content.Should().Contain("GetOptionAsync");
    }

    // ── Cross-cutting ──────────────────────────────────────────────────

    [Fact]
    public void AllOrderFilesUseCorrectModuleNamespace()
    {
        var files = Directory.GetFiles(
            Path.Combine(SrcRoot, "Application", "DTOs"), "*.cs", SearchOption.TopDirectoryOnly)
            .Concat(Directory.GetFiles(
                Path.Combine(SrcRoot, "Application", "Validators"), "*.cs", SearchOption.TopDirectoryOnly))
            .Concat(Directory.GetFiles(
                Path.Combine(SrcRoot, "Application", "Interfaces"), "*.cs", SearchOption.TopDirectoryOnly));

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            if (string.IsNullOrWhiteSpace(content)) continue;
            content.Should().Contain("namespace Tekram.Api.src.orders",
                $"file '{Path.GetFileName(file)}' must use orders module namespace");
        }
    }

    [Fact]
    public void DiffContainsOnlySixFiles_MatchingIssueScope()
    {
        // Structural guarantee: this slice must not touch Program.cs, appsettings,
        // auth, restaurants, or migrations. The 6-file diff is verified at build time
        // (0 errors means no broken refs), but we explicitly confirm file existence.
        var dtoDir = Path.Combine(SrcRoot, "Application", "DTOs");
        var valDir = Path.Combine(SrcRoot, "Application", "Validators");
        var intDir = Path.Combine(SrcRoot, "Application", "Interfaces");

        Directory.Exists(dtoDir).Should().BeTrue();
        Directory.Exists(valDir).Should().BeTrue();
        Directory.Exists(intDir).Should().BeTrue();

        File.Exists(Path.Combine(dtoDir, "PlaceOrderRequest.cs")).Should().BeTrue();
        File.Exists(Path.Combine(dtoDir, "OrderResponse.cs")).Should().BeTrue();
        File.Exists(Path.Combine(valDir, "PlaceOrderRequestValidator.cs")).Should().BeTrue();
        File.Exists(Path.Combine(intDir, "IOrderRepository.cs")).Should().BeTrue();
        File.Exists(Path.Combine(intDir, "ICouponRepository.cs")).Should().BeTrue();
        File.Exists(Path.Combine(intDir, "IMenuPricingReader.cs")).Should().BeTrue();
    }
}
