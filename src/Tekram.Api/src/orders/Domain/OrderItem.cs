namespace Tekram.Api.src.orders.Domain;

public class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid MenuItemId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPriceUsd { get; set; }
    public string? Customizations { get; set; }
    public decimal LineTotalUsd { get; set; }
}
