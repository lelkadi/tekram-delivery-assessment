using System.Xml.Linq;
using FluentAssertions;

namespace Tekram.Architecture.Tests.Infrastructure;

[Trait("issue", "7")]
public class ProjectScaffoldTests
{
    private static readonly string RepoRoot = FindRepoRoot();

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

    // AC1: Create .sln file — dotnet new sln -n Tekram
    [Fact]
    public void AC1_SolutionFileExistsWithCorrectName()
    {
        var slnPath = Path.Combine(RepoRoot, "Tekram.sln");
        File.Exists(slnPath).Should().BeTrue("Tekram.sln must exist at the repository root");

        var content = File.ReadAllText(slnPath);
        content.Should().Contain("Tekram.Api", "solution must reference the API project");
        content.Should().Contain("Tekram.Tests", "solution must reference the test project");
    }

    // AC2: Create web project — dotnet new web -n Tekram.Api -o src/Tekram.Api
    [Fact]
    public void AC2_WebProjectExistsWithCorrectSdk()
    {
        var csprojPath = Path.Combine(RepoRoot, "src", "Tekram.Api", "Tekram.Api.csproj");
        File.Exists(csprojPath).Should().BeTrue("Tekram.Api.csproj must exist");

        var xml = XDocument.Load(csprojPath);
        var sdk = xml.Root?.Attribute("Sdk")?.Value;
        sdk.Should().Be("Microsoft.NET.Sdk.Web", "API project must use the Web SDK");

        var targetFramework = xml.Descendants("TargetFramework").FirstOrDefault()?.Value;
        targetFramework.Should().Be("net8.0", "API project must target net8.0 LTS (TD-004)");
    }

    // AC3: Create test project — dotnet new xunit -n Tekram.Tests -o tests/Tekram.Tests
    [Fact]
    public void AC3_TestProjectExistsWithXunit()
    {
        var csprojPath = Path.Combine(RepoRoot, "tests", "Tekram.Tests", "Tekram.Tests.csproj");
        File.Exists(csprojPath).Should().BeTrue("Tekram.Tests.csproj must exist");

        var xml = XDocument.Load(csprojPath);
        var sdk = xml.Root?.Attribute("Sdk")?.Value;
        sdk.Should().Be("Microsoft.NET.Sdk", "test project must use the standard SDK");

        var targetFramework = xml.Descendants("TargetFramework").FirstOrDefault()?.Value;
        targetFramework.Should().Be("net8.0", "test project must target net8.0 LTS (TD-004)");

        var packageRefs = xml.Descendants("PackageReference")
            .Select(p => p.Attribute("Include")?.Value ?? "")
            .ToList();
        packageRefs.Should().Contain("xunit", "test project must reference xunit");
    }

    // AC4: Add both projects to solution + project reference from tests to API
    [Fact]
    public void AC4_TestProjectReferencesApiProject()
    {
        var csprojPath = Path.Combine(RepoRoot, "tests", "Tekram.Tests", "Tekram.Tests.csproj");
        var xml = XDocument.Load(csprojPath);

        var projectRefs = xml.Descendants("ProjectReference")
            .Select(p => p.Attribute("Include")?.Value ?? "")
            .ToList();
        projectRefs.Should().Contain(r => r.Contains("Tekram.Api"),
            "test project must have a ProjectReference to Tekram.Api");
    }

    // AC5: Install NuGet packages per blueprint §2.2
    [Fact]
    public void AC5_ApiProjectHasAllRequiredNuGetPackages()
    {
        var csprojPath = Path.Combine(RepoRoot, "src", "Tekram.Api", "Tekram.Api.csproj");
        var xml = XDocument.Load(csprojPath);
        var packages = xml.Descendants("PackageReference")
            .Select(p => (name: p.Attribute("Include")?.Value ?? "", version: p.Attribute("Version")?.Value ?? ""))
            .ToDictionary(p => p.name, p => p.version);

        // Required API packages per blueprint §2.2
        packages.Should().ContainKey("BCrypt.Net-Next");
        packages.Should().ContainKey("FluentValidation.AspNetCore");
        packages.Should().ContainKey("Microsoft.AspNetCore.Authentication.JwtBearer");
        packages.Should().ContainKey("Microsoft.EntityFrameworkCore.Design");
        packages.Should().ContainKey("Npgsql.EntityFrameworkCore.PostgreSQL");
        packages.Should().ContainKey("Scalar.AspNetCore");
        packages.Should().ContainKey("Serilog.AspNetCore");
        packages.Should().ContainKey("Serilog.Sinks.Console");
        packages.Should().ContainKey("StackExchange.Redis");

        // Framework-tied packages must be on the 8.x line (TD-004)
        packages["Microsoft.AspNetCore.Authentication.JwtBearer"].Should().StartWith("8.");
        packages["Microsoft.EntityFrameworkCore.Design"].Should().StartWith("8.");
        packages["Npgsql.EntityFrameworkCore.PostgreSQL"].Should().StartWith("8.");
        packages["Serilog.AspNetCore"].Should().StartWith("8.");
    }

    [Fact]
    public void AC5_TestProjectHasAllRequiredNuGetPackages()
    {
        var csprojPath = Path.Combine(RepoRoot, "tests", "Tekram.Tests", "Tekram.Tests.csproj");
        var xml = XDocument.Load(csprojPath);
        var packages = xml.Descendants("PackageReference")
            .Select(p => (name: p.Attribute("Include")?.Value ?? "", version: p.Attribute("Version")?.Value ?? ""))
            .ToDictionary(p => p.name, p => p.version);

        // Required test packages per blueprint §2.2
        packages.Should().ContainKey("Microsoft.AspNetCore.Mvc.Testing");
        packages.Should().ContainKey("FluentAssertions");
        packages.Should().ContainKey("xunit");
        packages.Should().ContainKey("Microsoft.NET.Test.Sdk");

        // Mvc.Testing must be on the 8.x line (TD-004)
        packages["Microsoft.AspNetCore.Mvc.Testing"].Should().StartWith("8.");
    }

    // AC6: Create the full directory tree per blueprint §2.3
    [Fact]
    public void AC6_FullDirectoryTreeExists()
    {
        var srcRoot = Path.Combine(RepoRoot, "src", "Tekram.Api", "src");

        // Core module directories
        var requiredDirs = new[]
        {
            "shared",
            "auth/Domain",
            "auth/Application/DTOs",
            "auth/Application/Handlers",
            "auth/Application/Interfaces",
            "auth/Application/Validators",
            "auth/Infrastructure",
            "auth/Presentation",
            "restaurants/Domain",
            "restaurants/Application/DTOs",
            "restaurants/Application/Handlers",
            "restaurants/Application/Interfaces",
            "restaurants/Application/Validators",
            "restaurants/Infrastructure",
            "restaurants/Presentation",
            "orders/Domain",
            "orders/Application/DTOs",
            "orders/Application/Handlers",
            "orders/Application/Interfaces",
            "orders/Application/Validators",
            "orders/Infrastructure",
            "orders/Presentation",
        };

        foreach (var dir in requiredDirs)
        {
            var fullPath = Path.Combine(srcRoot, dir);
            Directory.Exists(fullPath).Should().BeTrue(
                $"directory '{dir}' must exist under src/Tekram.Api/src/");
        }
    }

    [Fact]
    public void AC6_TestDirectoryStructureExists()
    {
        var testRoot = Path.Combine(RepoRoot, "tests", "Tekram.Tests");

        var requiredDirs = new[]
        {
            "auth",
            "restaurants",
            "orders",
            "Fixtures",
        };

        foreach (var dir in requiredDirs)
        {
            var fullPath = Path.Combine(testRoot, dir);
            Directory.Exists(fullPath).Should().BeTrue(
                $"directory '{dir}' must exist under tests/Tekram.Tests/");
        }
    }

    // AC7: Docker Compose — verify docker compose up -d works
    [Fact]
    public async Task AC7_DockerComposeStackIsRunning()
    {
        // Verify docker compose file exists
        var composePath = Path.Combine(RepoRoot, "docker-compose.yml");
        File.Exists(composePath).Should().BeTrue("docker-compose.yml must exist at repo root");

        // Verify postgres and redis are reachable via the lane config
        var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
                    ?? "Host=localhost;Port=5432;Database=tekram_lane2;Username=postgres;Password=postgres";

        // PostgreSQL check
        var dbHost = "localhost";
        var dbPort = 5432;
        if (dbUrl.Contains("Host="))
        {
            var match = System.Text.RegularExpressions.Regex.Match(dbUrl, @"Host=([^;]+)");
            if (match.Success) dbHost = match.Groups[1].Value;
        }

        using var pgClient = new System.Net.Sockets.TcpClient();
        await pgClient.ConnectAsync(dbHost, dbPort);
        pgClient.Connected.Should().BeTrue($"PostgreSQL must be reachable at {dbHost}:{dbPort}");

        // Redis check
        using var redisClient = new System.Net.Sockets.TcpClient();
        await redisClient.ConnectAsync("localhost", 6379);
        redisClient.Connected.Should().BeTrue("Redis must be reachable at localhost:6379");
    }

    // Additional spec compliance: global.json must pin SDK 8.0.303
    [Fact]
    public void GlobalJsonPinsSdkVersion80303()
    {
        var globalJsonPath = Path.Combine(RepoRoot, "global.json");
        File.Exists(globalJsonPath).Should().BeTrue("global.json must exist at repo root");

        var content = File.ReadAllText(globalJsonPath);
        content.Should().Contain("8.0.303", "global.json must pin SDK to 8.0.303");
    }
}
