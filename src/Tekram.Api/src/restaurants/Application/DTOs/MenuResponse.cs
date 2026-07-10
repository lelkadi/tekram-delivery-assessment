namespace Tekram.Api.src.restaurants.Application.DTOs;

public record MenuOptionResponse(
    Guid OptionId,
    string Name,
    decimal PriceModifierUsd
);

public record MenuCustomizationGroupResponse(
    Guid GroupId,
    string GroupName,
    bool IsRequired,
    int MaxSelections,
    List<MenuOptionResponse> Options
);

public record MenuItemResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal PriceUsd,
    bool IsAvailable,
    List<MenuCustomizationGroupResponse> CustomizationGroups
);

public record MenuCategoryResponse(
    Guid CategoryId,
    string CategoryName,
    int DisplayOrder,
    List<MenuItemResponse> Items
);

public record MenuResponse(
    Guid RestaurantId,
    List<MenuCategoryResponse> Categories
);
