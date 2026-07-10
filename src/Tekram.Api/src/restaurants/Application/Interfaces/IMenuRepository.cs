namespace Tekram.Api.src.restaurants.Application.Interfaces;

using Tekram.Api.src.restaurants.Domain;

public interface IMenuRepository
{
    Task<List<MenuCategory>> GetCategoriesByRestaurantAsync(Guid restaurantId, CancellationToken ct = default);
    Task<List<MenuItem>> GetItemsByCategoryAsync(Guid categoryId, CancellationToken ct = default);
    Task<MenuItem?> GetItemByIdAsync(Guid itemId, CancellationToken ct = default);
    Task<List<CustomizationGroup>> GetCustomizationGroupsByItemAsync(Guid menuItemId, CancellationToken ct = default);
    Task<List<CustomizationOption>> GetOptionsByGroupAsync(Guid groupId, CancellationToken ct = default);
    Task<List<MenuItem>> GetItemsByRestaurantAsync(Guid restaurantId, CancellationToken ct = default);
}
