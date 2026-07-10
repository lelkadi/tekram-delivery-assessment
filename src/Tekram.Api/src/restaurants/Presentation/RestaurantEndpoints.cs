namespace Tekram.Api.src.restaurants.Presentation;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Tekram.Api.src.restaurants.Application.DTOs;
using Tekram.Api.src.restaurants.Application.Handlers;

public static class RestaurantEndpoints
{
    public static RouteGroupBuilder MapRestaurantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/food/restaurants");

        // Verified against blueprint §§5.5–5.7

        // GET /api/food/restaurants
        group.MapGet("/", async (
            [AsParameters] SearchRestaurantsRequest request,
            SearchRestaurantsHandler handler,
            CancellationToken ct) =>
        {
            var response = await handler.HandleAsync(request, ct);
            return Results.Ok(response);
        })
        .WithName("SearchRestaurants")
        .WithOpenApi();

        // GET /api/food/restaurants/{id}/menu
        group.MapGet("/{id:guid}/menu", async (
            Guid id,
            GetMenuHandler handler,
            CancellationToken ct) =>
        {
            var response = await handler.HandleAsync(id, ct);
            return Results.Ok(response);
        })
        .WithName("GetRestaurantMenu")
        .WithOpenApi();

        return group;
    }
}
