namespace Tekram.Api.src.orders.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Tekram.Api.src.orders.Application.Interfaces;
using Tekram.Api.src.restaurants.Domain;
using Tekram.Api.src.shared;

public class MenuPricingReader : IMenuPricingReader
{
    private readonly TekramDbContext _db;

    public MenuPricingReader(TekramDbContext db)
    {
        _db = db;
    }

    public async Task<MenuItem?> GetItemForPricingAsync(Guid menuItemId, CancellationToken ct = default)
    {
        return await _db.MenuItems.FirstOrDefaultAsync(i => i.Id == menuItemId, ct);
    }

    public async Task<List<CustomizationGroup>> GetCustomizationGroupsAsync(Guid menuItemId,
        CancellationToken ct = default)
    {
        return await _db.CustomizationGroups
            .Where(g => g.MenuItemId == menuItemId)
            .ToListAsync(ct);
    }

    public async Task<CustomizationOption?> GetOptionAsync(Guid optionId,
        CancellationToken ct = default)
    {
        return await _db.CustomizationOptions.FirstOrDefaultAsync(o => o.Id == optionId, ct);
    }
}
