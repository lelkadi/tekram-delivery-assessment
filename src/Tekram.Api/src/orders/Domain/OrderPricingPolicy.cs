namespace Tekram.Api.src.orders.Domain;

public static class OrderPricingPolicy
{
    public const decimal MinimumOrderValueUsd = 7.00m;
    public const decimal SmallOrderSurchargeUsd = 1.00m;
    public const decimal BaseDeliveryFeeUsd = 1.50m;

    public static decimal CalculateSmallOrderSurcharge(decimal subtotalUsd)
    {
        return subtotalUsd < MinimumOrderValueUsd ? SmallOrderSurchargeUsd : 0m;
    }

    public static decimal CalculateDeliveryFee(decimal latitude, decimal longitude)
    {
        return BaseDeliveryFeeUsd;
    }

    public static decimal ApplyCoupon(decimal subtotalUsd, Coupon coupon)
    {
        if (subtotalUsd < coupon.MinSubtotalUsd)
            return 0m;

        return coupon.DiscountType == "percent"
            ? Math.Round(subtotalUsd * coupon.DiscountValue / 100m, 2)
            : Math.Min(coupon.DiscountValue, subtotalUsd);
    }

    public static decimal CalculateTotal(decimal subtotalUsd, decimal deliveryFeeUsd,
        decimal smallOrderSurchargeUsd, decimal discountUsd)
    {
        var total = subtotalUsd + deliveryFeeUsd + smallOrderSurchargeUsd - discountUsd;
        return Math.Max(total, 0m);
    }
}
