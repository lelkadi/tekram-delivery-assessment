namespace Tekram.Api.src.orders.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Tekram.Api.src.orders.Application.Interfaces;
using Tekram.Api.src.orders.Domain;
using Tekram.Api.src.shared;

public sealed class CouponRepository : ICouponRepository
{
    private readonly TekramDbContext _db;

    public CouponRepository(TekramDbContext db)
    {
        _db = db;
    }

    public async Task<Coupon?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        return await _db.Coupons
            .FirstOrDefaultAsync(c => c.Code == code && c.Active, ct);
    }

    public async Task IncrementUsageAsync(Coupon coupon, CancellationToken ct = default)
    {
        coupon.UsesCount++;
        _db.Coupons.Update(coupon);
        await _db.SaveChangesAsync(ct);
    }
}
