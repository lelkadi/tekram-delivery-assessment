namespace Tekram.Api.src.orders.Application.Interfaces;

using Tekram.Api.src.orders.Domain;

public interface ICouponRepository
{
    Task<Coupon?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task IncrementUsageAsync(Coupon coupon, CancellationToken ct = default);
}
