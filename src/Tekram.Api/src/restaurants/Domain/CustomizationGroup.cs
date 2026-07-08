namespace Tekram.Api.src.restaurants.Domain;

public class CustomizationGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public int MaxSelections { get; set; } = 1;
}
