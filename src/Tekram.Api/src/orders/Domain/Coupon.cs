namespace Tekram.Api.src.orders.Domain;

public class Coupon
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string DiscountType { get; set; } = "percent";
    public decimal DiscountValue { get; set; }
    public decimal MinSubtotalUsd { get; set; }
    public int? MaxUses { get; set; }
    public int UsesCount { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidUntil { get; set; }
    public bool Active { get; set; } = true;
}
