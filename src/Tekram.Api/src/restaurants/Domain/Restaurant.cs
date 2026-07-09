namespace Tekram.Api.src.restaurants.Domain;

public class Restaurant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Cuisine { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public int PriceTier { get; set; }
    public int AvgPrepMinutes { get; set; }
    public string Status { get; set; } = "active";
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}
