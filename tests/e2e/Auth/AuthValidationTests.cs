namespace Tekram.E2E.Auth;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

/// <summary>
/// Black-box negative-path coverage for issue #63 (validation half): an invalid payload
/// against a FluentValidation-guarded endpoint must produce HTTP 422 with the RFC7807
/// problem envelope the API uses (error: validation_failed, errors[] of field/message).
/// </summary>
[Trait("issue", "63")]
public class AuthValidationTests : LiveApiTestBase
{
    private static async Task<JsonElement> GetJson(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<JsonElement>())!;

    [LiveFact]
    public async Task AC1_InvalidRegisterPayload_Returns422WithValidationStructure()
    {
        // Violates every register rule: malformed email, non-Lebanese phone, weak password.
        var resp = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "x",
            email = "not-an-email",
            phone = "1",
            password = "a",
            role = "customer",
        });

        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "invalid payloads must return 422, not 400/500");

        var body = await GetJson(resp);
        body.GetProperty("status").GetInt32().Should().Be(422);
        body.GetProperty("error").GetString().Should().Be("validation_failed");

        var errors = body.GetProperty("errors").EnumerateArray().ToList();
        errors.Should().NotBeEmpty("the envelope must carry per-field validation errors");
        foreach (var e in errors)
        {
            e.GetProperty("field").GetString().Should().NotBeNullOrEmpty();
            e.GetProperty("message").GetString().Should().NotBeNullOrEmpty();
        }
        errors.Select(e => e.GetProperty("field").GetString()).Should()
            .Contain(["Email", "Phone", "Password"],
                "each violated rule must be reported against its field");
    }
}
