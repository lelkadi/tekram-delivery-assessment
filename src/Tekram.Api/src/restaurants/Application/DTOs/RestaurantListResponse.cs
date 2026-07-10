namespace Tekram.Api.src.restaurants.Application.DTOs;

using Tekram.Api.src.shared;

public record RestaurantListItem(
    Guid Id,
    string Name,
    string? Description,
    string Cuisine,
    decimal Rating,
    int AveragePrepTimeMinutes,
    int PriceTier,
    decimal Latitude,
    decimal Longitude,
    string Status
);

public record RestaurantListResponse(
    List<RestaurantListItem> Data,
    PaginationResponse Pagination
);
