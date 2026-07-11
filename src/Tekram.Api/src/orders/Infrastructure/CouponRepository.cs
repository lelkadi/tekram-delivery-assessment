namespace Tekram.Api.src.orders.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Tekram.Api.src.orders.Application.Interfaces;
using Tekram.Api.src.orders.Domain;
using Tekram.Api.src.shared;

public class CouponRepository : ICouponRepository
{
    private readonly TekramDbContext _db;

    public CouponRepository(TekramDbContext db)
    {
        _db = db;
    }

    public async Task<Coupon?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        return await _db.Coupons.FirstOrDefaultAsync(c => c.Code == code, ct);
    }

    // NOTE: IncrementUsageAsync is retained for ICouponRepository interface compliance
    // but is no longer called by the handler — coupon atomicity is handled inline
    // in PlaceOrderHandler via coupon.UsesCount++ within the order's SaveChangesAsync.
    public async Task IncrementUsageAsync(Coupon coupon, CancellationToken ct = default)
    {
        coupon.UsesCount++;
        await _db.SaveChangesAsync(ct);
    }
}
