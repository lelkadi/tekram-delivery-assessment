namespace Tekram.Api.src.orders.Application.DTOs;

public record CustomizationChoice(
    Guid GroupId,
    Guid OptionId
);

public record OrderItemRequest(
    Guid MenuItemId,
    int Quantity,
    List<CustomizationChoice>? CustomizationChoices = null
);

public record PlaceOrderRequest(
    Guid RestaurantId,
    List<OrderItemRequest> Items,
    string DeliveryAddress,
    string PaymentMethod,
    string? CouponCode,
    string? SpecialInstructions
);
