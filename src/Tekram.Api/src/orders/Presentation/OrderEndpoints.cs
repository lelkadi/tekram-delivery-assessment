// Verified against blueprint §§6.5-6.7
namespace Tekram.Api.src.orders.Presentation;

using System.Security.Claims;
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
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirstValue("sub")!);
            var response = await handler.HandleAsync(userId, request, ct);
            return Results.Created($"/api/food/orders/{response.BookingId}", response);
        })
        .RequireAuthorization()
        .WithName("PlaceOrder")
        .WithOpenApi();

        return group;
    }
}
