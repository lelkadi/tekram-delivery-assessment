namespace Tekram.Api.src.orders.Application.Interfaces;

using Tekram.Api.src.restaurants.Domain;

public interface IMenuPricingReader
{
    Task<MenuItem?> GetItemForPricingAsync(Guid menuItemId, CancellationToken ct = default);
    Task<List<CustomizationGroup>> GetCustomizationGroupsAsync(Guid menuItemId, CancellationToken ct = default);
    Task<CustomizationOption?> GetOptionAsync(Guid optionId, CancellationToken ct = default);
}
