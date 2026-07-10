namespace Tekram.Api.src.restaurants.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Tekram.Api.src.restaurants.Application.Interfaces;
using Tekram.Api.src.restaurants.Domain;
using Tekram.Api.src.shared;

public class MenuRepository : IMenuRepository
{
    private readonly TekramDbContext _db;

    public MenuRepository(TekramDbContext db)
    {
        _db = db;
    }

    public async Task<List<MenuCategory>> GetCategoriesByRestaurantAsync(Guid restaurantId,
        CancellationToken ct = default)
    {
        return await _db.MenuCategories
            .Where(c => c.RestaurantId == restaurantId)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(ct);
    }

    public async Task<List<MenuItem>> GetItemsByCategoryAsync(Guid categoryId,
        CancellationToken ct = default)
    {
        return await _db.MenuItems
            .Where(i => i.CategoryId == categoryId)
            .ToListAsync(ct);
    }

    public async Task<MenuItem?> GetItemByIdAsync(Guid itemId, CancellationToken ct = default)
    {
        return await _db.MenuItems.FirstOrDefaultAsync(i => i.Id == itemId, ct);
    }

    public async Task<List<CustomizationGroup>> GetCustomizationGroupsByItemAsync(Guid menuItemId,
        CancellationToken ct = default)
    {
        return await _db.CustomizationGroups
            .Where(g => g.MenuItemId == menuItemId)
            .ToListAsync(ct);
    }

    public async Task<List<CustomizationOption>> GetOptionsByGroupAsync(Guid groupId,
        CancellationToken ct = default)
    {
        return await _db.CustomizationOptions
            .Where(o => o.GroupId == groupId)
            .ToListAsync(ct);
    }

    public async Task<List<MenuItem>> GetItemsByRestaurantAsync(Guid restaurantId,
        CancellationToken ct = default)
    {
        return await _db.MenuItems
            .Where(i => i.RestaurantId == restaurantId && i.DeletedAt == null)
            .ToListAsync(ct);
    }
}
