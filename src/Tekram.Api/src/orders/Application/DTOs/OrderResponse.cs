namespace Tekram.Api.src.orders.Application.DTOs;

public record OrderTotalsResponse(
    decimal SubtotalUsd,
    decimal DeliveryFeeUsd,
    decimal SmallOrderSurchargeUsd,
    decimal DiscountUsd,
    decimal TotalUsd
);

public record OrderResponse(
    Guid BookingId,
    string Status,
    OrderTotalsResponse Totals,
    DateTime CreatedAt
);
