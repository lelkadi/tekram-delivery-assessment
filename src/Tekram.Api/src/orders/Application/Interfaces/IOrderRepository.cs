namespace Tekram.Api.src.orders.Application.Interfaces;

using Tekram.Api.src.orders.Domain;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken ct = default);
}
