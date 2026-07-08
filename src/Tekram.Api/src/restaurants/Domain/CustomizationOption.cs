namespace Tekram.Api.src.restaurants.Domain;

public class CustomizationOption
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PriceModifierUsd { get; set; }
}
