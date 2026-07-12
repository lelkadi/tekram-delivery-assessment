namespace Tekram.Tests.Fixtures;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Tekram.Api.src.auth.Application.DTOs;
using Tekram.Api.src.auth.Application.Interfaces;
using Tekram.Api.src.auth.Domain;
using Tekram.Api.src.shared;

public static class AuthHelper
{
    /// <summary>
    /// Registers a new user via the API and inserts known OTP codes ("123456") directly into the DB.
    /// Returns the HttpClient, JWT token, and user ID. Does NOT verify email/phone — use
    /// <see cref="RegisterVerifiedUserAsync"/> for flows that require a verified user.
    /// </summary>
    public static async Task<(HttpClient Client, string Token, Guid UserId)> RegisterAndGetToken(
        CustomWebApplicationFactory factory)
    {
        var client = factory.CreateClient();

        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var registerBody = new
        {
            name = "Test User",
            email = $"test{uniqueSuffix}@test.com",
            phone = $"+96170{Random.Shared.Next(10000, 99999)}",
            password = "Password1",
            role = "customer"
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", registerBody);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        ArgumentNullException.ThrowIfNull(auth);

        // Insert known OTP codes (hashed "123456") directly into the DB
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TekramDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var hash = hasher.Hash("123456");

        db.OtpCodes.Add(new OtpCode
        {
            Id = Guid.NewGuid(),
            UserId = auth.Id,
            Channel = "email",
            CodeHash = hash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow
        });

        db.OtpCodes.Add(new OtpCode
        {
            Id = Guid.NewGuid(),
            UserId = auth.Id,
            Channel = "phone",
            CodeHash = hash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        return (client, auth.Token, auth.Id);
    }

    /// <summary>
    /// Registers a new user, inserts OTP codes directly into DB,
    /// verifies both email and phone channels, then re-logins to get a verified token.
    /// Returns an authenticated HttpClient with Authorization header set.
    /// </summary>
    public static async Task<(HttpClient Client, string Token, Guid UserId)> RegisterVerifiedUserAsync(
        CustomWebApplicationFactory factory)
    {
        var client = factory.CreateClient();

        // Step 1: Register
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"test{uniqueSuffix}@test.com";
        var phone = $"+96170{Random.Shared.Next(10000, 99999)}";
        var password = "Password1";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Test User",
            email,
            phone,
            password,
            role = "customer"
        });
        registerResponse.EnsureSuccessStatusCode();
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        ArgumentNullException.ThrowIfNull(auth);

        // Step 2: Insert known OTP codes (hash of "123456") directly into DB
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TekramDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var hash = hasher.Hash("123456");

            db.OtpCodes.Add(new OtpCode
            {
                Id = Guid.NewGuid(), UserId = auth.Id, Channel = "email",
                CodeHash = hash, ExpiresAt = DateTime.UtcNow.AddMinutes(10), CreatedAt = DateTime.UtcNow
            });
            db.OtpCodes.Add(new OtpCode
            {
                Id = Guid.NewGuid(), UserId = auth.Id, Channel = "phone",
                CodeHash = hash, ExpiresAt = DateTime.UtcNow.AddMinutes(10), CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        // Step 3: Verify email (requires auth token from registration)
        var regClient = factory.CreateClient();
        regClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.Token);

        await regClient.PostAsJsonAsync("/api/auth/verify/email", new { code = "123456" });
        await regClient.PostAsJsonAsync("/api/auth/verify/phone", new { code = "123456" });

        // Step 4: Re-login to get a verified token
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            identifier = email,
            password
        });
        loginResponse.EnsureSuccessStatusCode();
        var loginAuth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        ArgumentNullException.ThrowIfNull(loginAuth);

        // Step 5: Create authenticated client with verified token
        var authClient = factory.CreateClient();
        authClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginAuth.Token);

        return (authClient, loginAuth.Token, loginAuth.Id);
    }
}
