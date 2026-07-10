namespace Tekram.E2E.Orders;

using FluentAssertions;

/// <summary>
/// Structural verification for issue #17 (Part 2 Slice 3.3 — Coupon seed data,
/// orders DI registration, endpoint mapping). Verifies seed data, DI wiring,
/// and Program.cs endpoint mapping.
/// </summary>
[Trait("issue", "17")]
public class OrdersCouponSeedTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Tekram.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Repo root not found");
    }

    private static string ReadAll(string path) =>
        File.ReadAllText(Path.Combine(RepoRoot, path));

    // ── AC1: 5 coupon seed records ─────────────────────────────────────

    [Fact]
    public void AC1_FiveCouponsInDbInitializer()
    {
        var content = ReadAll("src/Tekram.Api/src/shared/DbInitializer.cs");

        content.Should().Contain("WELCOME10");
        content.Should().Contain("FREEDELIVERY");
        content.Should().Contain("SUMMER10");
        content.Should().Contain("FREESHIP");
        content.Should().Contain("EXPIRED50");

        // EXPIRED50 must be inactive
        content.Should().Contain("Active = false");
    }

    [Fact]
    public void AC1_CouponVariantsExist()
    {
        var content = ReadAll("src/Tekram.Api/src/shared/DbInitializer.cs");

        // Percent-based coupons
        content.Should().Contain("\"percent\"");
        // Fixed-value coupons
        content.Should().Contain("\"fixed\"");
        // Unlimited uses (FREESHIP: MaxUses = null)
        content.Should().Contain("MaxUses = null");
    }

    // ── AC2: Orders DI registration ────────────────────────────────────

    [Fact]
    public void AC2_OrderInterfacesRegisteredInDI()
    {
        var content = ReadAll("src/Tekram.Api/src/shared/ServiceCollectionExtensions.cs");

        content.Should().Contain("AddScoped<IOrderRepository, OrderRepository>");
        content.Should().Contain("AddScoped<ICouponRepository, CouponRepository>");
        content.Should().Contain("AddScoped<IMenuPricingReader, MenuPricingReader>");
        content.Should().Contain("AddScoped<orders.Application.Handlers.PlaceOrderHandler>");

        // Must NOT be commented out
        content.Should().NotContain("// services.AddScoped<IOrderRepository");
    }

    // ── AC3: Endpoint mapping in Program.cs ─────────────────────────────

    [Fact]
    public void AC3_MapOrderEndpoints_Uncommented()
    {
        var content = ReadAll("src/Tekram.Api/Program.cs");

        content.Should().Contain("app.MapOrderEndpoints()");
        // Must NOT be commented out
        content.Should().NotContain("// app.MapOrderEndpoints");
    }

    [Fact]
    public void AC3_OrdersPresentationImport_Present()
    {
        var content = ReadAll("src/Tekram.Api/Program.cs");

        content.Should().Contain("using Tekram.Api.src.orders.Presentation;");
        content.Should().NotContain("// using Tekram.Api.src.orders.Presentation;");
    }

    // ── Cross-cutting ──────────────────────────────────────────────────

    [Fact]
    public void AllOrdersDiUncommented()
    {
        var content = ReadAll("src/Tekram.Api/src/shared/ServiceCollectionExtensions.cs");

        // The DI block for orders (#15-#17) must have all 4 registrations uncommented
        var lines = content.Split('\n');
        var inOrdersBlock = false;
        foreach (var line in lines)
        {
            if (line.Contains("Orders infrastructure") || line.Contains("Orders handlers"))
                inOrdersBlock = true;
            if (inOrdersBlock && line.TrimStart().StartsWith("//") && line.Contains("AddScoped"))
                Assert.Fail($"Commented-out DI registration found: {line.Trim()}");
        }
    }
}
