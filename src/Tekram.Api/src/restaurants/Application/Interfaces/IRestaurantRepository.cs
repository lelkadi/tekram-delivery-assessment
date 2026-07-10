namespace Tekram.Api.src.restaurants.Application.Interfaces;

using Tekram.Api.src.restaurants.Domain;

public interface IRestaurantRepository
{
    Task<(IReadOnlyList<Restaurant> Items, int TotalCount)> SearchAsync(
        string? search, string? cuisine, int? priceTier, int page, int limit,
        CancellationToken ct = default);

    Task<Restaurant?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
