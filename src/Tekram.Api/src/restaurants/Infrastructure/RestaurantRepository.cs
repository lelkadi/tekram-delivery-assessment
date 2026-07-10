namespace Tekram.Api.src.restaurants.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Tekram.Api.src.restaurants.Application.Interfaces;
using Tekram.Api.src.restaurants.Domain;
using Tekram.Api.src.shared;

public class RestaurantRepository : IRestaurantRepository
{
    private readonly TekramDbContext _db;

    public RestaurantRepository(TekramDbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<Restaurant> Items, int TotalCount)> SearchAsync(
        string? search, string? cuisine, int? priceTier, int page, int limit,
        CancellationToken ct = default)
    {
        var query = _db.Restaurants
            .Where(r => r.Status == "active" && r.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(cuisine))
            query = query.Where(r => r.Cuisine == cuisine);

        if (priceTier.HasValue)
            query = query.Where(r => r.PriceTier == priceTier.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = query.Where(r => EF.Functions.ILike(r.Name, pattern));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(r => r.Rating).ThenBy(r => r.Id)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<Restaurant?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Restaurants.FirstOrDefaultAsync(r => r.Id == id, ct);
    }
}
