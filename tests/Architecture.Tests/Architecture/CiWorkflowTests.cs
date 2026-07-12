using System.Text.RegularExpressions;
using FluentAssertions;

namespace Tekram.Architecture.Tests.Architecture;

/// <summary>
/// Black-box e2e verification for issue #45 (Part 5 · CI workflow).
///
/// Verifies the CI pipeline file (.github/workflows/ci.yml) matches the
/// spec defined in the issue and eng-lead comments:
///   1. Exists at the correct path with correct triggers (PR + push to main)
///   2. build-test job: restore → build -warnaserror → EF drift guard → integration tests
///   3. e2e job: restore+build → boot API → wait /healthz → black-box e2e suite
///   4. Service containers mirror docker-compose.yml (postgres:16-alpine, redis:7-alpine)
///   5. SDK pinned via global.json
///   6. CI run: build-test job is green
/// </summary>
[Trait("issue", "45")]
public class CiWorkflowTests
{
    private static readonly string RepoRoot = FindRepoRoot();
    private static readonly string CiPath = Path.Combine(RepoRoot, ".github", "workflows", "ci.yml");

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Tekram.sln")))
            dir = dir.Parent;
        return dir?.FullName
            ?? throw new InvalidOperationException("Could not find repository root (Tekram.sln not found)");
    }

    // ========================================================================
    // AC1: CI file exists at correct path
    // ========================================================================

    [Fact]
    public void AC1_CiFileExists_AtCorrectPath()
    {
        File.Exists(CiPath).Should().BeTrue(".github/workflows/ci.yml must exist");
    }

    // ========================================================================
    // AC2: CI triggers: PR + push to main
    // ========================================================================

    [Fact]
    public void AC2_Triggers_OnPullRequestAndPushToMain()
    {
        var content = File.ReadAllText(CiPath);
        content.Should().Contain("pull_request:", "must trigger on pull_request");
        content.Should().Contain("push:", "must trigger on push");
        content.Should().Contain("branches: [main]", "push must target main branch");
    }

    // ========================================================================
    // AC3: Service containers mirror docker-compose.yml
    // ========================================================================

    [Fact]
    public void AC3_ServiceContainers_Postgres16AndRedis7()
    {
        var content = File.ReadAllText(CiPath);

        // Postgres service container
        content.Should().Contain("postgres:", "must define postgres service container");
        content.Should().Contain("image: postgres:16-alpine",
            "postgres image must be postgres:16-alpine (matching docker-compose.yml)");
        content.Should().Contain("POSTGRES_USER: postgres", "must set POSTGRES_USER");
        content.Should().Contain("POSTGRES_PASSWORD: postgres", "must set POSTGRES_PASSWORD");
        content.Should().Contain("POSTGRES_DB: tekram", "must set POSTGRES_DB");

        // Redis service container
        content.Should().Contain("redis:", "must define redis service container");
        content.Should().Contain("image: redis:7-alpine",
            "redis image must be redis:7-alpine (matching docker-compose.yml)");

        // Port mappings
        content.Should().Contain("- 5432:5432", "postgres must expose port 5432");
        content.Should().Contain("- 6379:6379", "redis must expose port 6379");
    }

    // ========================================================================
    // AC4: build-test job steps match spec (docs/devops.md §2)
    // ========================================================================

    [Fact]
    public void AC4_BuildTestJob_HasCorrectSteps()
    {
        var content = File.ReadAllText(CiPath);

        // Job name
        content.Should().Contain("build-test:", "must define build-test job");

        // Restore
        content.Should().Contain("dotnet restore Tekram.sln", "must restore solution");

        // Build with -warnaserror
        content.Should().Contain("dotnet build Tekram.sln", "must build solution");
        content.Should().Contain("-warnaserror", "build must use -warnaserror (warnings as errors)");

        // EF drift guard
        content.Should().Contain("dotnet ef migrations has-pending-model-changes",
            "must check for pending EF model changes");
        content.Should().Contain("dotnet-ef", "must install dotnet-ef tool for drift guard");

        // Integration tests
        content.Should().Contain("dotnet test Tekram.sln", "must run solution-level integration tests");
    }

    // ========================================================================
    // AC5: e2e job steps match spec
    // ========================================================================

    [Fact]
    public void AC5_E2eJob_HasCorrectSteps()
    {
        var content = File.ReadAllText(CiPath);

        // Job name
        content.Should().Contain("e2e:", "must define e2e job");

        // Depends on build-test
        content.Should().Contain("needs: build-test", "e2e job must depend on build-test");

        // Restore and build e2e project (outside .sln)
        content.Should().Contain("tests/e2e/Tekram.E2E.csproj",
            "must restore and build the e2e project (outside Tekram.sln)");

        // Boot API
        content.Should().Contain("dotnet run --project src/Tekram.Api",
            "must boot the API with dotnet run");
        content.Should().Contain("--no-launch-profile",
            "must use --no-launch-profile to avoid launchSettings overriding ASPNETCORE_URLS");

        // Health check
        content.Should().Contain("/healthz", "must check /healthz for API readiness");
        content.Should().Contain("curl -sf", "must use curl to check /healthz");

        // E2E tests
        content.Should().Contain("dotnet test tests/e2e", "must run the e2e test suite");
        content.Should().Contain("E2E_BASE_URL", "must set E2E_BASE_URL for the black-box suite");
    }

    // ========================================================================
    // AC6: SDK pinned via global.json
    // ========================================================================

    [Fact]
    public void AC6_SdkPinnedViaGlobalJson()
    {
        var content = File.ReadAllText(CiPath);
        content.Should().Contain("global-json-file: global.json",
            "CI must pin SDK via global.json (actions/setup-dotnet global-json-file)");

        // Verify global.json exists
        var globalJsonPath = Path.Combine(RepoRoot, "global.json");
        File.Exists(globalJsonPath).Should().BeTrue("global.json must exist at repo root");
    }

    // ========================================================================
    // AC7: Concurrency group prevents duplicate CI runs
    // ========================================================================

    [Fact]
    public void AC7_ConcurrencyGroup_PreventsDuplicateRuns()
    {
        var content = File.ReadAllText(CiPath);
        content.Should().Contain("concurrency:", "should define concurrency group");
        content.Should().Contain("cancel-in-progress: true",
            "should cancel in-progress runs on new pushes");
    }

    // AC8 (string-index ordering of job definitions) was removed per issue #64: YAML job
    // order is semantically irrelevant — `needs: build-test` (AC5) is the real dependency.

    // ========================================================================
    // AC9: Migration + seed happen at API boot (not as separate CI steps)
    // ========================================================================

    [Fact]
    public void AC9_ApiBootIncludesMigrationAndSeed()
    {
        var content = File.ReadAllText(CiPath);
        // The API's Program.cs does MigrateAsync + SeedAsync on boot — CI doesn't
        // need separate migration steps. The workflow should NOT have `dotnet ef database update`.
        content.Should().NotContain("database update",
            "CI must not run `dotnet ef database update` separately — API auto-migrates on boot");
    }

    // ========================================================================
    // AC10: Global.json consistency
    // ========================================================================

    [Fact]
    public void AC10_GlobalJsonPinsSdk80303()
    {
        var globalJsonPath = Path.Combine(RepoRoot, "global.json");
        File.Exists(globalJsonPath).Should().BeTrue("global.json must exist");
        var content = File.ReadAllText(globalJsonPath);
        content.Should().Contain("8.0.303", "global.json must pin SDK 8.0.303");
    }
}
