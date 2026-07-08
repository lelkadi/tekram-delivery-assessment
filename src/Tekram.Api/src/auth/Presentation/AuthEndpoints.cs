namespace Tekram.Api.src.auth.Presentation;

using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Tekram.Api.src.auth.Application.DTOs;
using Tekram.Api.src.auth.Application.Handlers;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        // POST /api/auth/register
        group.MapPost("/register", async (
            RegisterRequest request,
            RegisterUserHandler handler,
            CancellationToken ct) =>
        {
            var response = await handler.HandleAsync(request, ct);
            return Results.Created($"/api/users/{response.Id}", response);
        })
        .WithName("Register")
        .WithTags("auth")
        .WithOpenApi();

        // POST /api/auth/login
        group.MapPost("/login", async (
            LoginRequest request,
            LoginHandler handler,
            CancellationToken ct) =>
        {
            var response = await handler.HandleAsync(request, ct);
            return Results.Ok(response);
        })
        .RequireRateLimiting("login")
        .WithName("Login")
        .WithTags("auth")
        .WithOpenApi();

        // POST /api/auth/verify/email
        group.MapPost("/verify/email", async (
            VerifyOtpRequest request,
            VerifyOtpHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            var response = await handler.HandleAsync(userId, "email", request, ct);
            return Results.Ok(response);
        })
        .RequireAuthorization()
        .WithName("VerifyEmail")
        .WithTags("auth")
        .WithOpenApi();

        // POST /api/auth/verify/phone
        group.MapPost("/verify/phone", async (
            VerifyOtpRequest request,
            VerifyOtpHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            var response = await handler.HandleAsync(userId, "phone", request, ct);
            return Results.Ok(response);
        })
        .RequireAuthorization()
        .WithName("VerifyPhone")
        .WithTags("auth")
        .WithOpenApi();

        // POST /api/auth/verify/resend
        group.MapPost("/verify/resend", async (
            ResendOtpRequest request,
            ResendOtpHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            await handler.HandleAsync(userId, request, ct);
            return Results.Ok(new { message = "OTP code resent successfully." });
        })
        .RequireAuthorization()
        .RequireRateLimiting("otp_resend")
        .WithName("ResendOtp")
        .WithTags("auth")
        .WithOpenApi();

        return group;
    }

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException("No 'sub' claim in token.");
        return Guid.Parse(sub);
    }
}
