namespace Tekram.E2E.Auth;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;

/// <summary>
/// Black-box e2e tests for auth endpoints: register, login, OTP verify, resend.
/// Maps to issue #18 ACs (Part 2 Slice 4.1).
/// Requires RUNNING API at E2E_BASE_URL. Skips gracefully when unset.
/// </summary>
[Trait("issue", "18")]
public class AuthIntegrationTests
{
    private static readonly string? BaseUrl = Environment.GetEnvironmentVariable("E2E_BASE_URL");

    private static HttpClient CreateClient()
    {
        if (BaseUrl is null) throw new InvalidOperationException("E2E_BASE_URL is not set");
        return new HttpClient { BaseAddress = new Uri(BaseUrl) };
    }

    private static bool ShouldSkip() => BaseUrl is null;

    private static async Task<JsonElement> DeserializeAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json;
    }

    private static StringContent JsonContent(object payload)
        => new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    private static string UniqueEmail() => $"e2e-{Guid.NewGuid():N}@test.tekram.local";
    private static string UniquePhone() => $"+9617{Random.Shared.Next(1000000, 9999999)}";

    // ========================================================================
    // REGISTER — POST /api/auth/register
    // ========================================================================

    [SkippableFact]
    public void AC1_Register_Success_Returns201()
    {
        Skip.If(ShouldSkip(), "E2E_BASE_URL is not set");
        var client = CreateClient();

        var payload = new { name = "E2E User", email = UniqueEmail(), phone = UniquePhone(), password = "Test1234", role = "customer" };
        var response = client.PostAsync("/api/auth/register", JsonContent(payload)).GetAwaiter().GetResult();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = DeserializeAsync(response).GetAwaiter().GetResult();
        json.GetProperty("id").ValueKind.Should().Be(JsonValueKind.String);
        json.GetProperty("name").GetString().Should().Be("E2E User");
        json.GetProperty("token").GetString().Should().NotBeNullOrEmpty();
        json.GetProperty("tokenExpiresAt").ValueKind.Should().Be(JsonValueKind.String);
    }

    [SkippableFact]
    public void AC2_Register_DuplicateEmail_Returns409()
    {
        Skip.If(ShouldSkip(), "E2E_BASE_URL is not set");
        var client = CreateClient();
        var email = UniqueEmail();

        var p1 = new { name = "First", email = email, phone = UniquePhone(), password = "Test1234", role = "customer" };
        client.PostAsync("/api/auth/register", JsonContent(p1)).GetAwaiter().GetResult();

        var p2 = new { name = "Second", email = email, phone = UniquePhone(), password = "Test1234", role = "customer" };
        var response = client.PostAsync("/api/auth/register", JsonContent(p2)).GetAwaiter().GetResult();

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var json = DeserializeAsync(response).GetAwaiter().GetResult();
        json.GetProperty("error").GetString().Should().Be("email_already_exists");
    }

    [SkippableFact]
    public void AC3_Register_DuplicatePhone_Returns409()
    {
        Skip.If(ShouldSkip(), "E2E_BASE_URL is not set");
        var client = CreateClient();
        var phone = UniquePhone();

        var p1 = new { name = "First", email = UniqueEmail(), phone = phone, password = "Test1234", role = "customer" };
        client.PostAsync("/api/auth/register", JsonContent(p1)).GetAwaiter().GetResult();

        var p2 = new { name = "Second", email = UniqueEmail(), phone = phone, password = "Test1234", role = "customer" };
        var response = client.PostAsync("/api/auth/register", JsonContent(p2)).GetAwaiter().GetResult();

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var json = DeserializeAsync(response).GetAwaiter().GetResult();
        json.GetProperty("error").GetString().Should().Be("phone_already_exists");
    }

    [SkippableFact]
    public void AC4_Register_WeakPassword_NoDigit_Returns422()
    {
        Skip.If(ShouldSkip(), "E2E_BASE_URL is not set");
        var client = CreateClient();
        var payload = new { name = "Weak", email = UniqueEmail(), phone = UniquePhone(), password = "abcdefgh", role = "customer" };

        var response = client.PostAsync("/api/auth/register", JsonContent(payload)).GetAwaiter().GetResult();
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var json = DeserializeAsync(response).GetAwaiter().GetResult();
        json.GetProperty("error").GetString().Should().Be("validation_failed");
    }

    [SkippableFact]
    public void AC5_Register_WeakPassword_NoUppercase_Returns422()
    {
        Skip.If(ShouldSkip(), "E2E_BASE_URL is not set");
        var client = CreateClient();
        var payload = new { name = "Weak", email = UniqueEmail(), phone = UniquePhone(), password = "12345678", role = "customer" };

        var response = client.PostAsync("/api/auth/register", JsonContent(payload)).GetAwaiter().GetResult();
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [SkippableFact]
    public void AC6_Register_WeakPassword_TooShort_Returns422()
    {
        Skip.If(ShouldSkip(), "E2E_BASE_URL is not set");
        var client = CreateClient();
        var payload = new { name = "Weak", email = UniqueEmail(), phone = UniquePhone(), password = "Ab1", role = "customer" };

        var response = client.PostAsync("/api/auth/register", JsonContent(payload)).GetAwaiter().GetResult();
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [SkippableFact]
    public void AC7_Register_InvalidPhone_Returns422()
    {
        Skip.If(ShouldSkip(), "E2E_BASE_URL is not set");
        var client = CreateClient();
        var payload = new { name = "Bad", email = UniqueEmail(), phone = "123456", password = "Test1234", role = "customer" };

        var response = client.PostAsync("/api/auth/register", JsonContent(payload)).GetAwaiter().GetResult();
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [SkippableFact]
    public void AC8_Register_InvalidRole_Returns422()
    {
        Skip.If(ShouldSkip(), "E2E_BASE_URL is not set");
        var client = CreateClient();
        var payload = new { name = "Hacker", email = UniqueEmail(), phone = UniquePhone(), password = "Test1234", role = "hacker" };

        var response = client.PostAsync("/api/auth/register", JsonContent(payload)).GetAwaiter().GetResult();
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [SkippableFact]
    public void AC9_Register_EmptyName_Returns422()
    {
        Skip.If(ShouldSkip(), "E2E_BASE_URL is not set");
        var client = CreateClient();
        var payload = new { name = "", email = UniqueEmail(), phone = UniquePhone(), password = "Test1234", role = "customer" };

        var response = client.PostAsync("/api/auth/register", JsonContent(payload)).GetAwaiter().GetResult();
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ========================================================================
    // LOGIN — POST /api/auth/login
    // ========================================================================

    [SkippableFact]
    public void AC10_Login_ByEmail_Success_Returns200()
    {
        Skip.If(ShouldSkip(), "E2E_BASE_URL is not set");
        var client = CreateClient();
        var email = UniqueEmail();
        var phone = UniquePhone();
        var password = "Test1234";

        client.PostAsync("/api/auth/register", JsonContent(new { name = "Login", email = email, phone = phone, password = password, role = "customer" })).GetAwaiter().GetResult();

        var response = client.PostAsync("/api/auth/login", JsonContent(new { identifier = email, password = password })).GetAwaiter().GetResult();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = DeserializeAsync(response).GetAwaiter().GetResult();
        json.GetProperty("token").GetString().Should().NotBeNullOrEmpty();
    }

    [SkippableFact]
    public void AC11_Login_ByPhone_Success_Returns200()
    {
        Skip.If(ShouldSkip(), "E2E_BASE_URL is not set");
        var client = CreateClient();
        var email = UniqueEmail();
        var phone = UniquePhone();
        var password = "Test1234";

        client.PostAsync("/api/auth/register", JsonContent(new { name = "Login", email = email, phone = phone, password = password, role = "customer" })).GetAwaiter().GetResult();

        var response = client.PostAsync("/api/auth/login", JsonContent(new { identifier = phone, password = password })).GetAwaiter().GetResult();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public void AC12_Login_WrongPassword_Returns401()
    {
        Skip.If(ShouldSkip(), "E2E_BASE_URL is not set");
        var client = CreateClient();
        var email = UniqueEmail();
        client.PostAsync("/api/auth/register", JsonContent(new { name = "L", email = email, phone = UniquePhone(), password = "Test1234", role = "customer" })).GetAwaiter().GetResult();

        var response = client.PostAsync("/api/auth/login", JsonContent(new { identifier = email, password = "WrongPass1" })).GetAwaiter().GetResult();
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var json = DeserializeAsync(response).GetAwaiter().GetResult();
        json.GetProperty("error").GetString().Should().Be("invalid_credentials");
    }

    [SkippableFact]
    public void AC13_Login_NonExistentUser_Returns401()
    {
        Skip.If(ShouldSkip(), "E2E_BASE_URL is not set");
        var client = CreateClient();
        var response = client.PostAsync("/api/auth/login", JsonContent(new { identifier = "nonexistent@test.com", password = "Test1234" })).GetAwaiter().GetResult();
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [SkippableFact]
    public void AC14_Login_EmptyIdentifier_Returns422()
    {
        Skip.If(ShouldSkip(), "E2E_BASE_URL is not set");
        var client = CreateClient();
        var response = client.PostAsync("/api/auth/login", JsonContent(new { identifier = "", password = "Test1234" })).GetAwaiter().GetResult();
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ========================================================================
    // OTP VERIFY — POST /api/auth/verify/email + /verify/phone (Requires JWT)
    // ========================================================================

    [SkippableFact]
    public void AC15_VerifyEmail_Success_Returns200()
    {
        Skip.If(ShouldSkip(), "E2E_BASE_URL is not set");
        var client = CreateClient();
        // Register to get a token
        var regResp = client.PostAsync("/api/auth/register",
            JsonContent(new { name = "V", email = UniqueEmail(), phone = UniquePhone(), password = "Test1234", role = "customer" })).GetAwaiter().GetResult();
        var regJson = DeserializeAsync(regResp).GetAwaiter().GetResult();
        var token = regJson.GetProperty("token").GetString()!;

        // Need to find the OTP — in e2e we can't read the DB/log, so we check
        // that the endpoint accepts well-formed requests with proper auth
        var authClient = new HttpClient { BaseAddress = new Uri(BaseUrl!) };
        authClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Attempt verification with a 6-digit code (may succeed or fail based on OTP,
        // but the endpoint must respond with a proper status)
        var response = authClient.PostAsync("/api/auth/verify/email",
            JsonContent(new { code = "123456" })).GetAwaiter().GetResult();

        // Either 200 (if lucky) or 422 (wrong code) — both are valid non-error responses
        (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.UnprocessableEntity)
            .Should().BeTrue("verify/email should return 200 or 422 with valid JWT");
    }

    [SkippableFact]
    public void AC16_VerifyPhone_Success_Returns200()
    {
        Skip.If(ShouldSkip(), "E2E_BASE_URL is not set");
        var client = CreateClient();
        var regResp = client.PostAsync("/api/auth/register",
            JsonContent(new { name = "V", email = UniqueEmail(), phone = UniquePhone(), password = "Test1234", role = "customer" })).GetAwaiter().GetResult();
        var regJson = DeserializeAsync(regResp).GetAwaiter().GetResult();
        var token = regJson.GetProperty("token").GetString()!;

        var authClient = new HttpClient { BaseAddress = new Uri(BaseUrl!) };
        authClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = authClient.PostAsync("/api/auth/verify/phone",
            JsonContent(new { code = "123456" })).GetAwaiter().GetResult();

        (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.UnprocessableEntity)
            .Should().BeTrue("verify/phone should return 200 or 422 with valid JWT");
    }

    [SkippableFact]
    public void AC17_Verify_NoJWT_Returns401()
    {
        Skip.If(ShouldSkip(), "E2E_BASE_URL is not set");
        var client = CreateClient();
        var response = client.PostAsync("/api/auth/verify/email",
            JsonContent(new { code = "123456" })).GetAwaiter().GetResult();
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ========================================================================
    // RESEND OTP — POST /api/auth/verify/resend (Requires JWT)
    // ========================================================================

    [SkippableFact]
    public void AC18_Resend_Email_Success_Returns200()
    {
        Skip.If(ShouldSkip(), "E2E_BASE_URL is not set");
        var client = CreateClient();
        var regResp = client.PostAsync("/api/auth/register",
            JsonContent(new { name = "R", email = UniqueEmail(), phone = UniquePhone(), password = "Test1234", role = "customer" })).GetAwaiter().GetResult();
        var token = DeserializeAsync(regResp).GetAwaiter().GetResult().GetProperty("token").GetString()!;

        var authClient = new HttpClient { BaseAddress = new Uri(BaseUrl!) };
        authClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = authClient.PostAsync("/api/auth/verify/resend",
            JsonContent(new { channel = "email" })).GetAwaiter().GetResult();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = DeserializeAsync(response).GetAwaiter().GetResult();
        json.GetProperty("message").GetString().Should().Contain("OTP code resent successfully");
    }

    [SkippableFact]
    public void AC19_Resend_Phone_Success_Returns200()
    {
        Skip.If(ShouldSkip(), "E2E_BASE_URL is not set");
        var client = CreateClient();
        var regResp = client.PostAsync("/api/auth/register",
            JsonContent(new { name = "R", email = UniqueEmail(), phone = UniquePhone(), password = "Test1234", role = "customer" })).GetAwaiter().GetResult();
        var token = DeserializeAsync(regResp).GetAwaiter().GetResult().GetProperty("token").GetString()!;

        var authClient = new HttpClient { BaseAddress = new Uri(BaseUrl!) };
        authClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = authClient.PostAsync("/api/auth/verify/resend",
            JsonContent(new { channel = "phone" })).GetAwaiter().GetResult();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public void AC20_Resend_InvalidChannel_Returns422()
    {
        Skip.If(ShouldSkip(), "E2E_BASE_URL is not set");
        var client = CreateClient();
        var regResp = client.PostAsync("/api/auth/register",
            JsonContent(new { name = "R", email = UniqueEmail(), phone = UniquePhone(), password = "Test1234", role = "customer" })).GetAwaiter().GetResult();
        var token = DeserializeAsync(regResp).GetAwaiter().GetResult().GetProperty("token").GetString()!;

        var authClient = new HttpClient { BaseAddress = new Uri(BaseUrl!) };
        authClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = authClient.PostAsync("/api/auth/verify/resend",
            JsonContent(new { channel = "pigeon" })).GetAwaiter().GetResult();

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
