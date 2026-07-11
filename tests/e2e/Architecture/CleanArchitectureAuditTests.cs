using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;

namespace Tekram.E2E.Architecture;

/// <summary>
/// Black-box e2e audit for issue #21 (Part 2 · Slice 4.4 — Clean Architecture polish).
///
/// Verifies the 7 audit categories from the issue's acceptance criteria:
///   1. Domain layer purity — ZERO framework imports
///   2. Application layer purity — no ASP.NET Core / EF Core / Infrastructure refs
///   3. Infrastructure implements Application interfaces
///   4. Presentation isolation — no DbContext in endpoints
///   5. Module isolation — Orders → Restaurants only via IMenuPricingReader
///   6. Code quality — async/await, file-scoped namespaces, decimal money, Guid IDs
///   7. No secrets in code
///
/// Plus regression: existing API endpoints still work after the cleanup.
/// </summary>
[Trait("issue", "21")]
public class CleanArchitectureAuditTests
{
    private static readonly string RepoRoot = FindRepoRoot();
    private static readonly string? BaseUrl = Environment.GetEnvironmentVariable("E2E_BASE_URL");
    private static readonly string SrcRoot = Path.Combine(RepoRoot, "src", "Tekram.Api", "src");

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Tekram.sln")))
            dir = dir.Parent;
        return dir?.FullName
            ?? throw new InvalidOperationException("Could not find repository root (Tekram.sln not found)");
    }

    // ========================================================================
    // CATEGORY 1: Domain layer purity — no framework/DB/Redis/Serilog imports
    // ========================================================================

    [Fact]
    public void AC1_DomainLayer_Auth_HasNoFrameworkImports()
    {
        var domainDir = Path.Combine(SrcRoot, "auth", "Domain");
        AssertDomainPurity(domainDir, "auth/Domain");
    }

    [Fact]
    public void AC1_DomainLayer_Restaurants_HasNoFrameworkImports()
    {
        var domainDir = Path.Combine(SrcRoot, "restaurants", "Domain");
        AssertDomainPurity(domainDir, "restaurants/Domain");
    }

    [Fact]
    public void AC1_DomainLayer_Orders_HasNoFrameworkImports()
    {
        var domainDir = Path.Combine(SrcRoot, "orders", "Domain");
        AssertDomainPurity(domainDir, "orders/Domain");
    }

    private static void AssertDomainPurity(string domainDir, string label)
    {
        Directory.Exists(domainDir).Should().BeTrue($"{label} directory must exist");

        var forbidden = new[] {
            "using Microsoft.AspNetCore.",
            "using Microsoft.EntityFrameworkCore",
            "using StackExchange.Redis",
            "using Serilog",
            "using FluentValidation"
        };

        foreach (var file in Directory.GetFiles(domainDir, "*.cs", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(file);
            foreach (var pattern in forbidden)
            {
                content.Should().NotContain(pattern,
                    $"{label}/{Path.GetFileName(file)} must NOT reference framework/DB/Redis/Serilog/FluentValidation. Found: {pattern}");
            }
        }
    }

    // ========================================================================
    // CATEGORY 2: Application layer purity — no ASP.NET Core, EF Core, Infrastructure refs
    // ========================================================================

    [Fact]
    public void AC2_ApplicationLayer_Auth_HasNoInfrastructureRefs()
    {
        var appDir = Path.Combine(SrcRoot, "auth", "Application");
        AssertApplicationPurity(appDir, "auth/Application");
    }

    [Fact]
    public void AC2_ApplicationLayer_Restaurants_HasNoInfrastructureRefs()
    {
        var appDir = Path.Combine(SrcRoot, "restaurants", "Application");
        AssertApplicationPurity(appDir, "restaurants/Application");
    }

    [Fact]
    public void AC2_ApplicationLayer_Orders_HasNoInfrastructureRefs()
    {
        var appDir = Path.Combine(SrcRoot, "orders", "Application");
        AssertApplicationPurity(appDir, "orders/Application");
    }

    [Fact]
    public void AC2_ApplicationLayer_NoFromQueryInDTOs()
    {
        // Specific fix from #21: SearchRestaurantsRequest had
        // [FromQuery(Name = "price_tier")] — a Microsoft.AspNetCore.Mvc ref in Application layer
        var dtoFile = Path.Combine(SrcRoot, "restaurants", "Application", "DTOs", "SearchRestaurantsRequest.cs");
        File.Exists(dtoFile).Should().BeTrue("SearchRestaurantsRequest.cs must exist");
        var content = File.ReadAllText(dtoFile);
        content.Should().NotContain("FromQuery",
            "[FromQuery] is an ASP.NET Core attribute and must NOT appear in the Application layer DTOs");
        content.Should().NotContain("Microsoft.AspNetCore.Mvc",
            "Application layer must not reference Microsoft.AspNetCore.Mvc");
    }

    private static void AssertApplicationPurity(string appDir, string label)
    {
        Directory.Exists(appDir).Should().BeTrue($"{label} directory must exist");

        var forbidden = new[] {
            "using Microsoft.AspNetCore.Mvc",    // ASP.NET Core MVC not in Application
            "Microsoft.EntityFrameworkCore.",    // EF Core directly not in Application
            ".Infrastructure",                   // No Infrastructure namespace refs
            ".Presentation"                      // No Presentation namespace refs
        };

        foreach (var file in Directory.GetFiles(appDir, "*.cs", SearchOption.AllDirectories))
        {
            var fileName = Path.GetFileName(file);
            var content = File.ReadAllText(file);

            // FluentValidation.AspNetCore is allowed in Application (validators)
            foreach (var pattern in forbidden)
            {
                if (content.Contains(pattern))
                {
                    // Allow FluentValidation.AspNetCore for validators
                    if (pattern == "Microsoft.AspNetCore.Mvc" && content.Contains("FluentValidation"))
                        continue;
                    Assert.Fail($"{label}/{fileName} contains forbidden reference: {pattern}");
                }
            }
        }
    }

    // ========================================================================
    // CATEGORY 3: Infrastructure implements Application interfaces
    // ========================================================================

    [Fact]
    public void AC3_AuthInfrastructure_ImplementsApplicationInterfaces()
    {
        var infraDir = Path.Combine(SrcRoot, "auth", "Infrastructure");
        var expected = new Dictionary<string, string>
        {
            ["UserRepository.cs"] = "IUserRepository",
            ["OtpRepository.cs"] = "IOtpRepository",
            ["BcryptPasswordHasher.cs"] = "IPasswordHasher",
            ["JwtTokenProvider.cs"] = "ITokenProvider",
            ["LoggingNotificationGateway.cs"] = "INotificationGateway",
        };

        foreach (var (file, iface) in expected)
        {
            var path = Path.Combine(infraDir, file);
            File.Exists(path).Should().BeTrue($"{file} must exist");
            var content = File.ReadAllText(path);
            content.Should().Contain(iface, $"{file} must implement {iface}");
        }
    }

    [Fact]
    public void AC3_RestaurantsInfrastructure_ImplementsApplicationInterfaces()
    {
        var infraDir = Path.Combine(SrcRoot, "restaurants", "Infrastructure");
        var expected = new Dictionary<string, string>
        {
            ["RestaurantRepository.cs"] = "IRestaurantRepository",
            ["MenuRepository.cs"] = "IMenuRepository",
        };

        foreach (var (file, iface) in expected)
        {
            var path = Path.Combine(infraDir, file);
            File.Exists(path).Should().BeTrue($"{file} must exist");
            var content = File.ReadAllText(path);
            content.Should().Contain(iface, $"{file} must implement {iface}");
        }
    }

    // ========================================================================
    // CATEGORY 4: Presentation isolation — no DbContext in endpoints
    // ========================================================================

    [Fact]
    public void AC4_AuthEndpoints_HasNoDbContext()
    {
        var epFile = Path.Combine(SrcRoot, "auth", "Presentation", "AuthEndpoints.cs");
        File.Exists(epFile).Should().BeTrue();
        var content = File.ReadAllText(epFile);
        content.Should().NotContain("TekramDbContext", "AuthEndpoints must not use DbContext directly");
        content.Should().NotContain("DbSet", "AuthEndpoints must not reference DbSet types");
    }

    [Fact]
    public void AC4_RestaurantEndpoints_HasNoDbContext()
    {
        var epFile = Path.Combine(SrcRoot, "restaurants", "Presentation", "RestaurantEndpoints.cs");
        File.Exists(epFile).Should().BeTrue();
        var content = File.ReadAllText(epFile);
        content.Should().NotContain("TekramDbContext", "RestaurantEndpoints must not use DbContext directly");
        content.Should().NotContain("DbSet", "RestaurantEndpoints must not reference DbSet types");
    }

    [Fact]
    public void AC4_Endpoints_DelegateToHandlers_NotBusinessLogic()
    {
        var authEp = Path.Combine(SrcRoot, "auth", "Presentation", "AuthEndpoints.cs");
        var restEp = Path.Combine(SrcRoot, "restaurants", "Presentation", "RestaurantEndpoints.cs");

        foreach (var epFile in new[] { authEp, restEp })
        {
            var content = File.ReadAllText(epFile);
            // Each endpoint should call handler.HandleAsync — not contain raw business logic
            content.Should().Contain("handler.HandleAsync",
                $"{Path.GetFileName(epFile)} must delegate to handler, not contain business logic");
        }
    }

    // ========================================================================
    // CATEGORY 5: Module isolation — Orders → Restaurants only via IMenuPricingReader
    // ========================================================================

    [Fact]
    public void AC5_OrdersModule_OnlyReferencesRestaurantsViaInterface()
    {
        var ordersDir = Path.Combine(SrcRoot, "orders");

        // IMenuPricingReader interface must exist (it's in orders/Application/Interfaces)
        var ifaceFile = Path.Combine(ordersDir, "Application", "Interfaces", "IMenuPricingReader.cs");
        File.Exists(ifaceFile).Should().BeTrue("IMenuPricingReader must exist");

        // Orders module should ONLY reference restaurants through this interface
        // Check: no direct references to restaurants.Infrastructure or restaurants.Presentation
        var files = Directory.GetFiles(ordersDir, "*.cs", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            var fileName = Path.GetFileName(file);

            // The IMenuPricingReader interface itself may reference restaurants.Domain (allowed)
            if (fileName == "IMenuPricingReader.cs")
                continue;

            content.Should().NotContain("restaurants.Infrastructure",
                $"{fileName} must NOT reference restaurants.Infrastructure directly");
            content.Should().NotContain("restaurants.Presentation",
                $"{fileName} must NOT reference restaurants.Presentation directly");
        }
    }

    // ========================================================================
    // CATEGORY 6: Code quality
    // ========================================================================

    [Fact]
    public void AC6_NoSyncOverAsync_NoResultNoWait()
    {
        var csFiles = Directory.GetFiles(SrcRoot, "*.cs", SearchOption.AllDirectories);
        foreach (var file in csFiles)
        {
            var content = File.ReadAllText(file);

            // Exclude test files and migrations
            if (file.Contains("/tests/") || file.Contains("/Migrations/"))
                continue;

            content.Should().NotContain(".Result",
                $"{Path.GetFileName(file)} must not use .Result (sync-over-async)");
            content.Should().NotContain(".Wait(",
                $"{Path.GetFileName(file)} must not use .Wait() (sync-over-async)");
        }
    }

    [Fact]
    public void AC6_FileScopedNamespaces_NoBlockNamespaces()
    {
        var csFiles = Directory.GetFiles(SrcRoot, "*.cs", SearchOption.AllDirectories);
        foreach (var file in csFiles)
        {
            // Migrations are auto-generated by EF Core tools — block-scoped namespaces are expected
            if (file.Contains("/Migrations/"))
                continue;

            var content = File.ReadAllText(file);

            // A block-scoped namespace looks like: namespace Foo { ... }
            // File-scoped looks like: namespace Foo;
            if (Regex.IsMatch(content, @"^namespace\s+\S+\s*\{", RegexOptions.Multiline))
            {
                Assert.Fail($"{Path.GetFileName(file)} uses block-scoped namespace. All non-migration files must use file-scoped namespaces.");
            }
        }
    }

    [Fact]
    public void AC6_NoNewtonsoft_SystemTextJsonOnly()
    {
        var csFiles = Directory.GetFiles(SrcRoot, "*.cs", SearchOption.AllDirectories);
        foreach (var file in csFiles)
        {
            var content = File.ReadAllText(file);
            content.Should().NotContain("Newtonsoft",
                $"{Path.GetFileName(file)} must not reference Newtonsoft.Json — use System.Text.Json");
        }
    }

    [Fact]
    public void AC6_PriceFieldsAreDecimal()
    {
        // All price/amount fields must be decimal (not float/double)
        var domainFiles = Directory.GetFiles(Path.Combine(SrcRoot, "restaurants", "Domain"), "*.cs")
            .Concat(Directory.GetFiles(Path.Combine(SrcRoot, "orders", "Domain"), "*.cs"));

        foreach (var file in domainFiles)
        {
            var content = File.ReadAllText(file);
            // Find properties with "Price" or "Amount" in name — they should be decimal
            var matches = Regex.Matches(content, @"public\s+(float|double)\s+\w*(Price|Amount|Usd)\w*");
            matches.Should().BeEmpty(
                $"{Path.GetFileName(file)} must not use float/double for monetary values — use decimal");
        }
    }

    [Fact]
    public void AC6_IdsAreGuid()
    {
        // All entity IDs must be Guid
        var domainFiles = Directory.GetFiles(Path.Combine(SrcRoot, "auth", "Domain"), "*.cs")
            .Concat(Directory.GetFiles(Path.Combine(SrcRoot, "restaurants", "Domain"), "*.cs"))
            .Concat(Directory.GetFiles(Path.Combine(SrcRoot, "orders", "Domain"), "*.cs"));

        foreach (var file in domainFiles)
        {
            var content = File.ReadAllText(file);
            // Find "Id" property — should be Guid, not int/long/string
            var matches = Regex.Matches(content, @"public\s+(int|long|string)\s+Id\s*\{");
            matches.Should().BeEmpty(
                $"{Path.GetFileName(file)} must use Guid for entity IDs, not int/long/string");
        }
    }

    [Fact]
    public void AC6_NoDateTimeNow_UsesUtcNow()
    {
        var csFiles = Directory.GetFiles(SrcRoot, "*.cs", SearchOption.AllDirectories);
        foreach (var file in csFiles)
        {
            if (file.Contains("/Migrations/") || file.Contains("/obj/"))
                continue;
            var content = File.ReadAllText(file);
            // "DateTime.Now" appears in comments/docs — only flag actual property usage followed by non-Utc
            // Use regex: DateTime.Now not followed by "Utc"
            var matches = Regex.Matches(content, @"\bDateTime\.Now\b(?!Utc)");
            if (matches.Count > 0)
            {
                Assert.Fail($"{Path.GetFileName(file)} uses DateTime.Now — use DateTime.UtcNow (found at line)");
            }
        }
    }

    // ========================================================================
    // CATEGORY 7: No secrets in code
    // ========================================================================

    [Fact]
    public void AC7_NoHardcodedSecrets()
    {
        var csFiles = Directory.GetFiles(SrcRoot, "*.cs", SearchOption.AllDirectories);
        var secretPatterns = new[]
        {
            @"\b(secret|password|key)\s*[:=]\s*""[^""]{8,}""",  // string literals
            @"JWT_SECRET\s*=\s*""",                               // env-style
            @"ConnectionString\s*=\s*""[^""]{8,}"""               // connection strings
        };

        foreach (var file in csFiles)
        {
            if (file.Contains("/appsettings") || file.Contains("/obj/"))
                continue;

            var content = File.ReadAllText(file);

            // Allow configuration reads: configuration["Jwt:Secret"], Configuration["Key"]
            // Flag actual hardcoded values only
            foreach (var pattern in secretPatterns)
            {
                foreach (Match match in Regex.Matches(content, pattern, RegexOptions.IgnoreCase))
                {
                    // Skip if the line also contains "configuration" or "IConfiguration"
                    var lineStart = content.LastIndexOf('\n', match.Index) + 1;
                    var lineEnd = content.IndexOf('\n', match.Index);
                    if (lineEnd == -1) lineEnd = content.Length;
                    var line = content[lineStart..lineEnd];

                    if (!line.Contains("configuration", StringComparison.OrdinalIgnoreCase) &&
                        !line.Contains("IConfiguration"))
                    {
                        Assert.Fail($"{Path.GetFileName(file)} contains a potential hardcoded secret: {match.Value.Trim()}");
                    }
                }
            }
        }
    }

    // ========================================================================
    // CONTRACT: net8.0 + MigrateAsync (PM rejection fixes)
    // ========================================================================

    [Fact]
    public void AC_RebaseFix_Net80InCsproj()
    {
        var apiProj = Path.Combine(RepoRoot, "src", "Tekram.Api", "Tekram.Api.csproj");
        var xml = XDocument.Load(apiProj);
        xml.Descendants("TargetFramework").FirstOrDefault()?.Value
            .Should().Be("net8.0", "PM rejection fix: must target net8.0 LTS (TD-004)");
    }

    [Fact]
    public void AC_RebaseFix_MigrateAsyncInProgramCs()
    {
        var programCs = Path.Combine(RepoRoot, "src", "Tekram.Api", "Program.cs");
        var content = File.ReadAllText(programCs);
        content.Should().Contain("MigrateAsync",
            "PM rejection fix: Program.cs must use MigrateAsync(), not EnsureCreatedAsync()");
        content.Should().NotContain("EnsureCreatedAsync",
            "PM rejection fix: Program.cs must NOT use EnsureCreatedAsync()");
    }

    // ========================================================================
    // REGRESSION: API endpoints still work after cleanup
    // ========================================================================

    [SkippableFact]
    public async Task AC_Regression_AuthRegister_ValidationWorks()
    {
        Skip.If(string.IsNullOrWhiteSpace(BaseUrl), "E2E_BASE_URL not set");
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl!) };
        client.Timeout = TimeSpan.FromSeconds(10);

        // Register with invalid phone — should get 422 validation error (API boots + auth routes work)
        var payload = new { name = "CATest", email = "ca.test@tekram.dev", phone = "invalid", password = "Test123!", role = "customer" };
        var response = await client.PostAsJsonAsync("/api/auth/register", payload);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "Auth endpoints must return validation errors after architecture cleanup");
    }

    [SkippableFact]
    public async Task AC_Regression_RestaurantBrowse_ReturnsData()
    {
        Skip.If(string.IsNullOrWhiteSpace(BaseUrl), "E2E_BASE_URL not set");
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl!) };
        client.Timeout = TimeSpan.FromSeconds(10);

        var response = await client.GetAsync("/api/food/restaurants");
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Restaurant browse must work after architecture cleanup");
    }

    [SkippableFact]
    public async Task AC_Regression_MenuEndpoint_ReturnsNestedStructure()
    {
        Skip.If(string.IsNullOrWhiteSpace(BaseUrl), "E2E_BASE_URL not set");
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl!) };
        client.Timeout = TimeSpan.FromSeconds(10);

        // Get first restaurant ID
        var list = await client.GetFromJsonAsync<JsonElement>("/api/food/restaurants?limit=1");
        var id = list.GetProperty("data")[0].GetProperty("id").GetGuid();

        var response = await client.GetAsync($"/api/food/restaurants/{id}/menu");
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Menu endpoint must work after architecture cleanup");

        var menu = await response.Content.ReadFromJsonAsync<JsonElement>();
        menu.GetProperty("categories").GetArrayLength().Should().BeGreaterThan(0,
            "Menu must contain categories after cleanup");
    }

    [SkippableFact]
    public async Task AC_Regression_PriceTier_CamelCaseWorks()
    {
        // priceTier (camelCase) MUST work — the backend field name
        Skip.If(string.IsNullOrWhiteSpace(BaseUrl), "E2E_BASE_URL not set");
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl!) };
        client.Timeout = TimeSpan.FromSeconds(10);

        var response = await client.GetAsync("/api/food/restaurants?priceTier=2");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = json.GetProperty("data").EnumerateArray().ToList();
        data.Should().NotBeEmpty();
        data.Should().OnlyContain(r => r.GetProperty("priceTier").GetInt32() == 2,
            "CamelCase priceTier filter must work after [FromQuery] removal");
    }

    [SkippableFact]
    public void AC_Regression_PriceTier_SnakeCaseContract()
    {
        // price_tier (snake_case) is the DOCUMENTED API contract.
        // This test documents the regression: removing [FromQuery(Name = "price_tier")]
        // broke snake_case binding. CamelCase priceTier works, but the contract says snake_case.
        Skip.If(string.IsNullOrWhiteSpace(BaseUrl), "E2E_BASE_URL not set");
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl!) };
        client.Timeout = TimeSpan.FromSeconds(10);

        var response = client.GetAsync("/api/food/restaurants?price_tier=2").GetAwaiter().GetResult();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = response.Content.ReadFromJsonAsync<JsonElement>().GetAwaiter().GetResult();
        var data = json.GetProperty("data").EnumerateArray().ToList();
        data.Should().NotBeEmpty();

        // The snake_case contract should work — if it doesn't, the data will include
        // restaurants with other price tiers (the filter was silently ignored)
        data.Should().OnlyContain(r => r.GetProperty("priceTier").GetInt32() == 2,
            "price_tier (snake_case) is the documented API contract — " +
            "removing [FromQuery(Name = \"price_tier\")] broke this binding. " +
            "All restaurants returned have non-matching tiers because the filter was ignored.");
    }
}
