using Microsoft.AspNetCore.Mvc;

namespace Tekram.Api.src.restaurants.Application.DTOs;

public record SearchRestaurantsRequest(
    string? Search,
    string? Cuisine,
    [property: FromQuery(Name = "price_tier")]
    int? PriceTier,
    int Page = 1,
    int Limit = 10
);
