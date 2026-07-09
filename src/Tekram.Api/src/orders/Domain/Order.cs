namespace Tekram.Api.src.orders.Domain;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid RestaurantId { get; set; }
    public Guid? CouponId { get; set; }
    public string Status { get; set; } = "pending";
    public string DeliveryAddress { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "COD";
    public decimal SubtotalUsd { get; set; }
    public decimal DeliveryFeeUsd { get; set; }
    public decimal SmallOrderSurchargeUsd { get; set; }
    public decimal DiscountUsd { get; set; }
    public decimal TotalUsd { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<OrderItem> OrderItems { get; set; } = new();
}
