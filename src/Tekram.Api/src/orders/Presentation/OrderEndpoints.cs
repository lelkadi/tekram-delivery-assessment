namespace Tekram.Api.src.orders.Presentation;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Tekram.Api.src.orders.Application.DTOs;
using Tekram.Api.src.orders.Application.Handlers;

public static class OrderEndpoints
{
    public static RouteGroupBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/food/orders");

        // POST /api/food/orders
        group.MapPost("/", async (
            PlaceOrderRequest request,
            PlaceOrderHandler handler,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            // In a real app this would come from the JWT claims
            var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Results.Unauthorized();

            var response = await handler.HandleAsync(userId, request, ct);
            return Results.Created($"/api/food/orders/{response.BookingId}", response);
        })
        .WithName("PlaceOrder")
        .WithOpenApi();

        return group;
    }
}
