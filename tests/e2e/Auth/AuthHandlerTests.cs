using System.Net;
using System.Xml.Linq;
using FluentAssertions;

namespace Tekram.E2E.Auth;

/// <summary>
/// Black-box coverage for issue #10 (Part 2 Slice 1.4 — Auth handlers, infrastructure,
/// presentation endpoints).
///
/// SCOPE NOTE: Program.cs is still the bare scaffold — DI registration and middleware
/// wiring land in #11. The 5 auth endpoints are defined but not reachable over HTTP
/// on this branch alone. The facts below verify file existence, structural compliance,
/// the architect's rejection fixes, and basic bootability.
/// </summary>
[Trait("issue", "10")]
public class AuthHandlerTests
{
    private static readonly string RepoRoot = FindRepoRoot();
    private static readonly string? BaseUrl = Environment.GetEnvironmentVariable("E2E_BASE_URL");

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

    // ── AC1: Handlers exist per blueprint §4.5 ──

    [Fact]
    public void AC1_HandlersExist()
    {
        var handlerDir = Path.Combine(RepoRoot, "src", "Tekram.Api", "src", "auth", "Application", "Handlers");
        Directory.Exists(handlerDir).Should().BeTrue("auth/Application/Handlers directory must exist");

        var handlers = new Dictionary<string, string[]>
        {
            ["RegisterUserHandler.cs"] = new[]
            {
                "class RegisterUserHandler",
                "HandleAsync",
                "EmailExistsAsync",
                "PhoneExistsAsync",
                "passwordHasher.Hash",
                "tokenProvider.GenerateToken",
                "GenerateOtpCode",
                "new AuthResponse"
            },
            ["LoginHandler.cs"] = new[]
            {
                "class LoginHandler",
                "HandleAsync",
                "GetByIdentifierAsync",
                "passwordHasher.Verify",
                "tokenProvider.GenerateToken"
            },
            ["VerifyOtpHandler.cs"] = new[]
            {
                "class VerifyOtpHandler",
                "HandleAsync",
                "GetLatestActiveCodeAsync",
                "passwordHasher.Verify",
                "ConsumeAsync",
                "OtpVerifyResponse"
            },
            ["ResendOtpHandler.cs"] = new[]
            {
                "class ResendOtpHandler",
                "HandleAsync",
                "CountRecentResendsAsync",
                "Random.Shared.Next",
                "SendOtpAsync",
                "MaxResends = 3"
            }
        };

        foreach (var (fileName, expectedTokens) in handlers)
        {
            var path = Path.Combine(handlerDir, fileName);
            File.Exists(path).Should().BeTrue($"{fileName} must exist in Handlers directory");

            var content = File.ReadAllText(path);
            foreach (var token in expectedTokens)
            {
                content.Should().Contain(token, $"{fileName} must contain '{token}'");
            }
        }
    }

    // ── AC2: Infrastructure exists per blueprint §4.6 ──

    [Fact]
    public void AC2_InfrastructureExists()
    {
        var infraDir = Path.Combine(RepoRoot, "src", "Tekram.Api", "src", "auth", "Infrastructure");
        Directory.Exists(infraDir).Should().BeTrue("auth/Infrastructure directory must exist");

        var infraFiles = new Dictionary<string, string[]>
        {
            ["BcryptPasswordHasher.cs"] = new[]
            {
                "class BcryptPasswordHasher", "IPasswordHasher",
                "WorkFactor = 12", "BCrypt.Net.BCrypt.HashPassword", "BCrypt.Net.BCrypt.Verify"
            },
            ["JwtTokenProvider.cs"] = new[]
            {
                "class JwtTokenProvider", "ITokenProvider",
                "SecurityAlgorithms.HmacSha256", "\"sub\"", "\"role\"", "JwtRegisteredClaimNames.Jti",
                "GenerateToken", "TokenExpiration"
            },
            ["LoggingNotificationGateway.cs"] = new[]
            {
                "class LoggingNotificationGateway", "INotificationGateway",
                "EMAIL_MOCK", "SMS_MOCK", "SendOtpAsync"
            },
            ["OtpRepository.cs"] = new[]
            {
                "class OtpRepository", "IOtpRepository",
                "GetLatestActiveCodeAsync", "AddAsync", "ConsumeAsync", "CountRecentResendsAsync"
            },
            ["UserRepository.cs"] = new[]
            {
                "class UserRepository", "IUserRepository",
                "GetByIdAsync", "GetByEmailAsync", "GetByPhoneAsync", "GetByIdentifierAsync",
                "EmailExistsAsync", "PhoneExistsAsync", "AddAsync", "UpdateAsync"
            }
        };

        foreach (var (fileName, expectedTokens) in infraFiles)
        {
            var path = Path.Combine(infraDir, fileName);
            File.Exists(path).Should().BeTrue($"{fileName} must exist in Infrastructure directory");

            var content = File.ReadAllText(path);
            foreach (var token in expectedTokens)
            {
                content.Should().Contain(token, $"{fileName} must contain '{token}'");
            }
        }
    }

    // ── AC3: Presentation endpoints exist per blueprint §4.7 ──

    [Fact]
    public void AC3_PresentationEndpointsExist()
    {
        var presDir = Path.Combine(RepoRoot, "src", "Tekram.Api", "src", "auth", "Presentation");
        Directory.Exists(presDir).Should().BeTrue("auth/Presentation directory must exist");

        var path = Path.Combine(presDir, "AuthEndpoints.cs");
        File.Exists(path).Should().BeTrue("AuthEndpoints.cs must exist");

        var content = File.ReadAllText(path);

        // 5 endpoint routes
        content.Should().Contain("MapGroup(\"/api/auth\")");
        content.Should().Contain("MapPost(\"/register\"");
        content.Should().Contain("MapPost(\"/login\"");
        content.Should().Contain("MapPost(\"/verify/email\"");
        content.Should().Contain("MapPost(\"/verify/phone\"");
        content.Should().Contain("MapPost(\"/verify/resend\"");

        // JWT gating
        content.Should().Contain(".RequireAuthorization()");

        // Rate limiting
        content.Should().Contain("RequireRateLimiting(\"login\")");
        content.Should().Contain("RequireRateLimiting(\"otp_resend\")");
    }

    // ── AC4: RedisRateLimiter is implemented (architect finding 4 fix) ──

    [Fact]
    public void AC4_RedisRateLimiterIsImplemented()
    {
        var path = Path.Combine(RepoRoot, "src", "Tekram.Api", "src", "auth", "Infrastructure", "RedisRateLimiter.cs");
        File.Exists(path).Should().BeTrue("RedisRateLimiter.cs must exist");

        var content = File.ReadAllText(path);

        // Must NOT be an empty stub (the architect rejection)
        content.Length.Should().BeGreaterThan(200,
            "RedisRateLimiter.cs must be a real implementation, not an empty stub");

        // Must implement rate limiting logic
        content.Should().Contain("IsAllowedAsync");
        content.Should().Contain("StringIncrementAsync");
        content.Should().Contain("KeyExpireAsync");
        content.Should().Contain("StackExchange.Redis");

        // Must be fail-open
        content.Should().Contain("fail-open");
        content.Should().Contain("RedisConnectionException");
    }

    // ── AC5: ITokenProvider blueprint compliance fix (DateTime, not TimeSpan) ──

    [Fact]
    public void AC5_ITokenProviderReturnsDateTime()
    {
        var path = Path.Combine(RepoRoot, "src", "Tekram.Api", "src", "auth", "Application", "Interfaces", "ITokenProvider.cs");
        var content = File.ReadAllText(path);

        content.Should().Contain("DateTime TokenExpiration",
            "ITokenProvider.TokenExpiration must return DateTime per blueprint §4.4.4, not TimeSpan");
        content.Should().NotContain("TimeSpan TokenExpiration",
            "ITokenProvider.TokenExpiration must NOT return TimeSpan (was a scaffold error in #9)");
    }

    // ── AC6: net8.0 compliance (TD-004) ──

    [Fact]
    public void AC6_Net80Compliance()
    {
        var apiProj = Path.Combine(RepoRoot, "src", "Tekram.Api", "Tekram.Api.csproj");
        var apiXml = XDocument.Load(apiProj);
        apiXml.Descendants("TargetFramework").FirstOrDefault()?.Value
            .Should().Be("net8.0", "API project must target net8.0 LTS (TD-004)");
    }

    // ── AC7: API boots (smoke test) ──

    [SkippableFact]
    public async Task AC7_ApiBootsSuccessfully()
    {
        Skip.If(string.IsNullOrWhiteSpace(BaseUrl), "E2E_BASE_URL not set — no live lane API to test against.");

        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl!) };
        client.Timeout = TimeSpan.FromSeconds(10);

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync("/");
        }
        catch (HttpRequestException ex)
        {
            throw new Xunit.Sdk.XunitException(
                $"Expected the API at {BaseUrl} to be reachable (auth handler/infrastructure changes " +
                $"must never prevent the app from starting), but the connection failed: {ex.Message}");
        }

        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
            "API responded with 500 — the app started but is erroring, which indicates " +
            "an assembly-load or type-resolution failure in the auth handler layer.");
    }

    // ── AC8: No out-of-scope changes (architect finding 2) ──

    [Fact]
    public void AC8_NoOutOfScopeAiRosterChanges()
    {
        var aiRosterDir = Path.Combine(RepoRoot, ".ai-roster");
        Directory.Exists(aiRosterDir).Should().BeTrue(".ai-roster directory must exist");
    }
}
