namespace Tekram.Api.src.restaurants.Application.Handlers;

using Tekram.Api.src.restaurants.Application.DTOs;
using Tekram.Api.src.restaurants.Application.Interfaces;
using Tekram.Api.src.shared;

public class GetMenuHandler
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IMenuRepository _menuRepository;

    public GetMenuHandler(IRestaurantRepository restaurantRepository, IMenuRepository menuRepository)
    {
        _restaurantRepository = restaurantRepository;
        _menuRepository = menuRepository;
    }

    public async Task<MenuResponse> HandleAsync(Guid restaurantId, CancellationToken ct = default)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(restaurantId, ct);

        if (restaurant is null || restaurant.Status != "active" || restaurant.DeletedAt.HasValue)
            throw new DomainException(404, "not_found", "Restaurant not found.");

        var categories = await _menuRepository.GetCategoriesByRestaurantAsync(restaurantId, ct);

        var categoryResponses = new List<MenuCategoryResponse>();

        foreach (var category in categories.OrderBy(c => c.DisplayOrder))
        {
            var items = await _menuRepository.GetItemsByCategoryAsync(category.Id, ct);
            var itemResponses = new List<MenuItemResponse>();

            foreach (var item in items.Where(i => !i.DeletedAt.HasValue))
            {
                var groups = await _menuRepository.GetCustomizationGroupsByItemAsync(item.Id, ct);
                var groupResponses = new List<MenuCustomizationGroupResponse>();

                foreach (var group in groups)
                {
                    var options = await _menuRepository.GetOptionsByGroupAsync(group.Id, ct);
                    groupResponses.Add(new MenuCustomizationGroupResponse(
                        GroupId: group.Id,
                        GroupName: group.Name,
                        IsRequired: group.IsRequired,
                        MaxSelections: group.MaxSelections,
                        Options: options.Select(o => new MenuOptionResponse(
                            OptionId: o.Id,
                            Name: o.Name,
                            PriceModifierUsd: o.PriceModifierUsd
                        )).ToList()
                    ));
                }

                itemResponses.Add(new MenuItemResponse(
                    Id: item.Id,
                    Name: item.Name,
                    Description: item.Description,
                    PriceUsd: item.PriceUsd,
                    IsAvailable: item.StockCount is null || item.StockCount > 0,
                    CustomizationGroups: groupResponses
                ));
            }

            categoryResponses.Add(new MenuCategoryResponse(
                CategoryId: category.Id,
                CategoryName: category.Name,
                DisplayOrder: category.DisplayOrder,
                Items: itemResponses
            ));
        }

        return new MenuResponse(RestaurantId: restaurantId, Categories: categoryResponses);
    }
}
