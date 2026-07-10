namespace Tekram.Api.src.orders.Infrastructure;

using Tekram.Api.src.orders.Application.Interfaces;
using Tekram.Api.src.orders.Domain;
using Tekram.Api.src.shared;

public class OrderRepository : IOrderRepository
{
    private readonly TekramDbContext _db;

    public OrderRepository(TekramDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        await _db.Orders.AddAsync(order, ct);
        await _db.SaveChangesAsync(ct);
    }
}
