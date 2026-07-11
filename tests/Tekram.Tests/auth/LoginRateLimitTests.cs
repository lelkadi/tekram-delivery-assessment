namespace Tekram.Tests.Auth;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

/// <summary>
/// Integration coverage for issue #51 — the login rate limiter must return HTTP 429
/// (not 503) and must partition per identifier+IP (Redis-backed, architecture §5)
/// instead of tripping one shared global bucket.
///
/// Runs against the live app + real Postgres/Redis for this lane (no mocking — TD/
/// architecture §9: only EMAIL_MOCK/SMS_MOCK are ever mocked). The API must already be
/// running at http://localhost:{PORT} (PORT from the lane's .lane-env) before this suite
/// executes, e.g.:
///   dotnet run --project src/Tekram.Api --no-launch-profile
///
/// NOTE: tests/Tekram.Tests/auth/AuthIntegrationTests.cs, Fixtures/AuthHelper.cs, and
/// CustomWebApplicationFactory.cs are empty placeholder stubs on this branch (verified via
/// `wc -l` — 0 bytes each; the populated versions only exist on the unmerged issue-18/
/// issue-16 branches). This file is deliberately self-contained — a raw HttpClient against
/// the running lane API — rather than an IClassFixture&lt;CustomWebApplicationFactory&gt;,
/// since that fixture does not exist yet on this branch.
/// </summary>
[Trait("issue", "51")]
public class LoginRateLimitTests
{
    private static readonly string BaseUrl =
        $"http://localhost:{Environment.GetEnvironmentVariable("PORT") ?? "3021"}";

    private static HttpClient NewClient() =>
        new() { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(10) };

    // ── AC1: a throttled login returns 429 (not 503) ──

    [Fact]
    public async Task Login_SameIdentifierSixAttempts_SixthReturns429()
    {
        using var client = NewClient();
        var identifier = $"ratelimit-{Guid.NewGuid():N}@test.com";

        HttpResponseMessage? last = null;
        for (var i = 0; i < 6; i++)
        {
            last = await client.PostAsJsonAsync("/api/auth/login",
                new { identifier, password = "WrongPassword1" });
        }

        last.Should().NotBeNull();
        last!.StatusCode.Should().Be((HttpStatusCode)429,
            "the 6th login attempt for the same identifier+IP within the 15-minute window must be throttled");
        last.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json",
            "the 429 must be an RFC7807 problem+json response");

        var body = await last.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("error").GetString().Should().Be("rate_limit_exceeded");
    }

    // ── AC2: the limiter partitions per identifier+IP, not one global bucket ──

    [Fact]
    public async Task Login_TwoDistinctIdentifiers_EachHasOwnBudget_NeitherThrottled()
    {
        using var client = NewClient();
        var identifierA = $"ratelimit-a-{Guid.NewGuid():N}@test.com";
        var identifierB = $"ratelimit-b-{Guid.NewGuid():N}@test.com";

        foreach (var identifier in new[] { identifierA, identifierB })
        {
            for (var attempt = 1; attempt <= 5; attempt++)
            {
                var response = await client.PostAsJsonAsync("/api/auth/login",
                    new { identifier, password = "WrongPassword1" });

                response.StatusCode.Should().NotBe((HttpStatusCode)429,
                    $"identifier '{identifier}' attempt {attempt}/5 is at or under its own limit and must not " +
                    "be throttled by another identifier's bucket — proves per-identifier+IP partitioning, not a shared global counter");
            }
        }
    }

    // ── AC4: no regression to the resend-cooldown 429 path (separate, handler/otp_resend-limiter enforced) ──

    [Fact]
    public async Task ResendOtp_StillEnforcesOwnRateLimit_NoRegression()
    {
        using var client = NewClient();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Rate Limit Test",
            email = $"ratelimit-resend-{suffix}@test.com",
            phone = $"+96170{Random.Shared.Next(10000, 99999)}",
            password = "Password1",
            role = "customer"
        });

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            "registration must succeed so we have a real authenticated user to exercise /verify/resend against");

        var auth = await registerResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = auth.GetProperty("token").GetString();

        using var authedClient = NewClient();
        authedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage? last = null;
        for (var i = 0; i < 4; i++)
        {
            last = await authedClient.PostAsJsonAsync("/api/auth/verify/resend", new { channel = "email" });
        }

        last.Should().NotBeNull();
        last!.StatusCode.Should().Be((HttpStatusCode)429,
            "the resend-cooldown path is untouched by this fix — it must still throttle after its own " +
            "3-per-15-minutes budget is exceeded, exactly as before");
    }
}
