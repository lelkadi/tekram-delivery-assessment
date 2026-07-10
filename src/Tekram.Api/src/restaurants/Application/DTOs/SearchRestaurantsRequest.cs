namespace Tekram.Api.src.restaurants.Application.DTOs;

public record SearchRestaurantsRequest(
    string? Search,
    string? Cuisine,
    int? PriceTier,
    int Page = 1,
    int Limit = 10
);
