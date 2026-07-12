namespace Tekram.E2E.Auth;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

/// <summary>
/// Black-box e2e flow for issue #61 — the primary auth happy path proven against the
/// running lane API: register a brand-new user, then log in with those credentials and
/// receive a JWT. Uses a unique identity per run so reruns never collide with earlier
/// rows (and never trip the per-identifier login rate limiter).
/// </summary>
[Trait("issue", "61")]
public class AuthFlowTests : LiveApiTestBase
{
    private static async Task<JsonElement> GetJson(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<JsonElement>())!;

    [LiveFact]
    public async Task AC1_RegisterThenLogin_ReturnsJwt()
    {
        var email = $"e2e-authflow-{Guid.NewGuid():N}@test.com";
        var phone = $"+961{70_000_000 + Random.Shared.Next(0, 9_999_999)}";
        const string password = "Test123!";

        var regResp = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "E2E Auth Flow",
            email,
            phone,
            password,
            role = "customer",
        });
        regResp.StatusCode.Should().Be(HttpStatusCode.Created,
            "registering a unique identity must succeed");

        var loginResp = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            identifier = email,
            password,
        });
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK,
            "login with the just-registered credentials must succeed");

        var body = await GetJson(loginResp);
        body.GetProperty("id").GetGuid().Should().NotBeEmpty();
        body.GetProperty("role").GetString().Should().Be("customer");

        var token = body.GetProperty("token").GetString();
        token.Should().NotBeNullOrWhiteSpace("login must return a JWT");
        token!.Split('.').Should().HaveCount(3,
            "a JWT is three dot-separated base64url segments (header.payload.signature)");
    }
}
