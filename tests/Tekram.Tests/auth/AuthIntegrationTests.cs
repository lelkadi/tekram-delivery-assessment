namespace Tekram.Tests.Auth;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tekram.Api.src.auth.Application.DTOs;
using Tekram.Api.src.auth.Application.Interfaces;
using Tekram.Api.src.auth.Domain;
using Tekram.Api.src.shared;
using Tekram.Tests.Fixtures;

public class AuthIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Generates a valid Lebanese phone number (+961 followed by exactly 7 digits).
    /// </summary>
    private static string ValidPhone()
        => $"+96170{Random.Shared.Next(10000, 99999)}";

    /// <summary>
    /// Generates a unique email suffix for test isolation.
    /// </summary>
    private static string UniqueEmail(string prefix)
        => $"{prefix}.{Guid.NewGuid():N}@test.com";

    // ========================================================================
    // Register Tests
    // ========================================================================

    [Fact]
    public async Task Register_Success_Returns201_AuthResponse()
    {
        // Arrange
        var client = _factory.CreateClient();
        var body = new
        {
            name = "Jane Doe",
            email = UniqueEmail("jane"),
            phone = ValidPhone(),
            password = "Passw0rd",
            role = "customer"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        auth!.Id.Should().NotBeEmpty();
        auth.Name.Should().Be(body.name);
        auth.Email.Should().Be(body.email.ToLowerInvariant());
        auth.Phone.Should().Be(body.phone);
        auth.Role.Should().Be(body.role);
        auth.Token.Should().NotBeNullOrEmpty();
        auth.TokenExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        // Arrange
        var client = _factory.CreateClient();
        var email = UniqueEmail("dup");
        var body = new
        {
            name = "First User",
            email,
            phone = ValidPhone(),
            password = "Passw0rd",
            role = "customer"
        };

        // Act — first register succeeds
        await client.PostAsJsonAsync("/api/auth/register", body);

        // Act — second with same email
        var body2 = new
        {
            name = "Second User",
            email,
            phone = ValidPhone(),
            password = "Passw0rd2",
            role = "customer"
        };
        var response = await client.PostAsJsonAsync("/api/auth/register", body2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("error").GetString().Should().Be("email_already_exists");
    }

    [Fact]
    public async Task Register_DuplicatePhone_Returns409()
    {
        // Arrange
        var client = _factory.CreateClient();
        var phone = ValidPhone();
        var body = new
        {
            name = "First User",
            email = UniqueEmail("first"),
            phone,
            password = "Passw0rd",
            role = "customer"
        };

        // Act — first register succeeds
        await client.PostAsJsonAsync("/api/auth/register", body);

        // Act — second with same phone
        var body2 = new
        {
            name = "Second User",
            email = UniqueEmail("second"),
            phone,
            password = "Passw0rd2",
            role = "customer"
        };
        var response = await client.PostAsJsonAsync("/api/auth/register", body2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("error").GetString().Should().Be("phone_already_exists");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("")]
    [InlineData("@no-local.com")]
    public async Task Register_InvalidEmail_Returns422(string invalidEmail)
    {
        // Arrange
        var client = _factory.CreateClient();
        var body = new
        {
            name = "Bad Email",
            email = invalidEmail,
            phone = ValidPhone(),
            password = "Passw0rd",
            role = "customer"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("error").GetString().Should().Be("validation_failed");
    }

    [Fact]
    public async Task Register_WeakPassword_NoDigit_Returns422()
    {
        // Arrange
        var client = _factory.CreateClient();
        var body = new
        {
            name = "Weak Password",
            email = UniqueEmail("weak"),
            phone = ValidPhone(),
            password = "Password", // no digit
            role = "customer"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("error").GetString().Should().Be("validation_failed");
    }

    [Fact]
    public async Task Register_WeakPassword_NoUppercase_Returns422()
    {
        // Arrange
        var client = _factory.CreateClient();
        var body = new
        {
            name = "Weak Password",
            email = UniqueEmail("weak2"),
            phone = ValidPhone(),
            password = "password1", // no uppercase
            role = "customer"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("error").GetString().Should().Be("validation_failed");
    }

    [Fact]
    public async Task Register_WeakPassword_TooShort_Returns422()
    {
        // Arrange
        var client = _factory.CreateClient();
        var body = new
        {
            name = "Weak Password",
            email = UniqueEmail("weak3"),
            phone = ValidPhone(),
            password = "Pass1", // only 6 chars
            role = "customer"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("error").GetString().Should().Be("validation_failed");
    }

    [Theory]
    [InlineData("+1234567890")]   // US number
    [InlineData("0096170000000")] // double-zero prefix
    [InlineData("not-a-phone")]
    [InlineData("")]
    public async Task Register_InvalidPhone_NonLebanese_Returns422(string invalidPhone)
    {
        // Arrange
        var client = _factory.CreateClient();
        var body = new
        {
            name = "Bad Phone",
            email = UniqueEmail("phone"),
            phone = invalidPhone,
            password = "Passw0rd",
            role = "customer"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("error").GetString().Should().Be("validation_failed");
    }

    [Fact]
    public async Task Register_InvalidRole_Returns422()
    {
        // Arrange
        var client = _factory.CreateClient();
        var body = new
        {
            name = "Bad Role",
            email = UniqueEmail("role"),
            phone = ValidPhone(),
            password = "Passw0rd",
            role = "superadmin"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("error").GetString().Should().Be("validation_failed");
    }

    [Fact]
    public async Task Register_EmptyName_Returns422()
    {
        // Arrange
        var client = _factory.CreateClient();
        var body = new
        {
            name = "",
            email = UniqueEmail("noname"),
            phone = ValidPhone(),
            password = "Passw0rd",
            role = "customer"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("error").GetString().Should().Be("validation_failed");
    }

    [Fact]
    public async Task Register_CreatesUserUnverified()
    {
        // Arrange
        var client = _factory.CreateClient();
        var body = new
        {
            name = "Unverified User",
            email = UniqueEmail("unverified"),
            phone = ValidPhone(),
            password = "Passw0rd",
            role = "customer"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();

        // Assert — verify directly from DB that user is unverified
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TekramDbContext>();
        var user = await db.Users.FindAsync(auth!.Id);
        user.Should().NotBeNull();
        user!.EmailVerified.Should().BeFalse();
        user.PhoneVerified.Should().BeFalse();
    }

    // ========================================================================
    // Login Tests
    // ========================================================================

    [Fact]
    public async Task Login_ByEmail_Success_Returns200()
    {
        // Arrange — register a user manually (need raw password)
        var client = _factory.CreateClient();
        var password = "Passw0rd";
        var email = UniqueEmail("login.email");
        var registerBody = new
        {
            name = "Login By Email",
            email,
            phone = ValidPhone(),
            password,
            role = "customer"
        };
        await client.PostAsJsonAsync("/api/auth/register", registerBody);

        // Act — login by email
        var loginBody = new { identifier = email, password };
        var response = await client.PostAsJsonAsync("/api/auth/login", loginBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        auth!.Email.Should().Be(email.ToLowerInvariant());
        auth.Name.Should().Be(registerBody.name);
        auth.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_ByPhone_Success_Returns200()
    {
        // Arrange — register a user
        var client = _factory.CreateClient();
        var password = "Passw0rd";
        var phone = ValidPhone();
        var registerBody = new
        {
            name = "Login By Phone",
            email = UniqueEmail("login.phone"),
            phone,
            password,
            role = "customer"
        };
        await client.PostAsJsonAsync("/api/auth/register", registerBody);

        // Act — login by phone
        var loginBody = new { identifier = phone, password };
        var response = await client.PostAsJsonAsync("/api/auth/login", loginBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        auth!.Phone.Should().Be(phone);
        auth.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        // Arrange — register a user
        var client = _factory.CreateClient();
        var email = UniqueEmail("login.wrongpwd");
        var registerBody = new
        {
            name = "Wrong Password",
            email,
            phone = ValidPhone(),
            password = "Passw0rd",
            role = "customer"
        };
        await client.PostAsJsonAsync("/api/auth/register", registerBody);

        // Act — wrong password
        var loginBody = new { identifier = email, password = "WrongPass1" };
        var response = await client.PostAsJsonAsync("/api/auth/login", loginBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("error").GetString().Should().Be("invalid_credentials");
    }

    [Fact]
    public async Task Login_NonExistentUser_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginBody = new
        {
            identifier = "nonexistent@test.com",
            password = "Passw0rd"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("error").GetString().Should().Be("invalid_credentials");
    }

    [Fact]
    public async Task Login_EmptyIdentifier_Returns422()
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginBody = new { identifier = "", password = "Passw0rd" };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("error").GetString().Should().Be("validation_failed");
    }

    [Fact]
    public async Task Login_RateLimitExceeded_Returns429()
    {
        // Use a separate factory to avoid consuming the shared fixture's rate limiter
        using var rateLimitFactory = new CustomWebApplicationFactory();
        var client = rateLimitFactory.CreateClient();

        // Send 5 rapid requests that pass the rate limiter but fail at handler
        for (int i = 0; i < 5; i++)
        {
            var body = new { identifier = $"nonexistent{i}@test.com", password = "Whatever1" };
            var resp = await client.PostAsJsonAsync("/api/auth/login", body);
            // These should be 401 (invalid credentials) — rate limiter allowed them
            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // Act — 6th request should be rate-limited
        var lastBody = new { identifier = "anyuser@test.com", password = "AnyPass1" };
        var lastResponse = await client.PostAsJsonAsync("/api/auth/login", lastBody);

        // Assert
        lastResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    // ========================================================================
    // Verify Email Tests
    // ========================================================================

    [Fact]
    public async Task VerifyEmail_Success_Returns200()
    {
        // Arrange — register and get a known OTP
        var (client, token, _) = await AuthHelper.RegisterAndGetToken(_factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var verifyBody = new { code = "123456" };
        var response = await client.PostAsJsonAsync("/api/auth/verify/email", verifyBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<OtpVerifyResponse>();
        result.Should().NotBeNull();
        result!.Channel.Should().Be("email");
        result.EmailVerified.Should().BeTrue();
        result.PhoneVerified.Should().BeFalse();
        result.FullyVerified.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyEmail_WrongCode_Returns422()
    {
        // Arrange
        var (client, token, _) = await AuthHelper.RegisterAndGetToken(_factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var verifyBody = new { code = "999999" };
        var response = await client.PostAsJsonAsync("/api/auth/verify/email", verifyBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("error").GetString().Should().Be("invalid_or_expired_code");
    }

    [Fact]
    public async Task VerifyEmail_AlreadyVerified_Returns422()
    {
        // Arrange — register and verify once
        var (client, token, _) = await AuthHelper.RegisterAndGetToken(_factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var verifyBody = new { code = "123456" };
        var firstResponse = await client.PostAsJsonAsync("/api/auth/verify/email", verifyBody);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act — verify again (same code, already consumed)
        var secondResponse = await client.PostAsJsonAsync("/api/auth/verify/email", verifyBody);

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var json = await secondResponse.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("error").GetString().Should().Be("invalid_or_expired_code");
    }

    [Fact]
    public async Task VerifyEmail_NoToken_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        // No auth header

        // Act
        var verifyBody = new { code = "123456" };
        var response = await client.PostAsJsonAsync("/api/auth/verify/email", verifyBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ========================================================================
    // Verify Phone Tests
    // ========================================================================

    [Fact]
    public async Task VerifyPhone_Success_Returns200()
    {
        // Arrange — register and get known OTPs
        var (client, token, _) = await AuthHelper.RegisterAndGetToken(_factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // First, verify email
        var emailVerify = new { code = "123456" };
        var emailResponse = await client.PostAsJsonAsync("/api/auth/verify/email", emailVerify);
        emailResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // The phone OTP from RegisterAndGetToken is still unconsumed, so we can use it directly.

        // Act — verify phone
        var phoneResponse = await client.PostAsJsonAsync("/api/auth/verify/phone", emailVerify);

        // Assert
        phoneResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await phoneResponse.Content.ReadFromJsonAsync<OtpVerifyResponse>();
        result.Should().NotBeNull();
        result!.Channel.Should().Be("phone");
        result.EmailVerified.Should().BeTrue();
        result.PhoneVerified.Should().BeTrue();
        result.FullyVerified.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyPhone_WrongCode_Returns422()
    {
        // Arrange
        var (client, token, _) = await AuthHelper.RegisterAndGetToken(_factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var verifyBody = new { code = "999999" };
        var response = await client.PostAsJsonAsync("/api/auth/verify/phone", verifyBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("error").GetString().Should().Be("invalid_or_expired_code");
    }

    // ========================================================================
    // Resend OTP Tests
    // ========================================================================

    [Fact]
    public async Task ResendOtp_Success_Returns200()
    {
        // Arrange
        var client = _factory.CreateClient();
        var registerBody = new
        {
            name = "Resend User",
            email = UniqueEmail("resend"),
            phone = ValidPhone(),
            password = "Passw0rd",
            role = "customer"
        };
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerBody);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        // Act
        var resendBody = new { channel = "email" };
        var response = await client.PostAsJsonAsync("/api/auth/verify/resend", resendBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("message").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ResendOtp_InvalidChannel_Returns422()
    {
        // Arrange
        var client = _factory.CreateClient();
        var registerBody = new
        {
            name = "Invalid Channel",
            email = UniqueEmail("channel"),
            phone = ValidPhone(),
            password = "Passw0rd",
            role = "customer"
        };
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerBody);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        // Act
        var resendBody = new { channel = "pigeon" };
        var response = await client.PostAsJsonAsync("/api/auth/verify/resend", resendBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("error").GetString().Should().Be("validation_failed");
    }

    [Fact]
    public async Task ResendOtp_RateLimitExceeded_Returns429()
    {
        // Use a separate factory to isolate rate limiter state
        using var rateLimitFactory = new CustomWebApplicationFactory();
        var client = rateLimitFactory.CreateClient();

        // Register a user
        var registerBody = new
        {
            name = "Rate Limited",
            email = UniqueEmail("ratelimit"),
            phone = ValidPhone(),
            password = "Passw0rd",
            role = "customer"
        };
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerBody);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        // Send 2 valid resend requests (the initial OTP from registration counts toward
        // MaxResends=3, so only 2 more resends are allowed by the handler)
        for (int i = 0; i < 2; i++)
        {
            var resendBody = new { channel = "email" };
            var resp = await client.PostAsJsonAsync("/api/auth/verify/resend", resendBody);
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // Act — 3rd request should be rate-limited (handler DomainException → 429)
        var lastBody = new { channel = "email" };
        var lastResponse = await client.PostAsJsonAsync("/api/auth/verify/resend", lastBody);

        // Assert
        lastResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
