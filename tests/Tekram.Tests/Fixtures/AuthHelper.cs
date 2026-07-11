namespace Tekram.Tests.Fixtures;

using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tekram.Api.src.auth.Application.DTOs;
using Tekram.Api.src.auth.Application.Interfaces;
using Tekram.Api.src.auth.Domain;
using Tekram.Api.src.shared;

public static class AuthHelper
{
    /// <summary>
    /// Registers a new user via the API and inserts known OTP codes ("123456") directly into the DB.
    /// Returns the HttpClient, JWT token, and user ID.
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
}
