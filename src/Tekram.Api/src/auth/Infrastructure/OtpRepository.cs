namespace Tekram.Api.src.auth.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Tekram.Api.src.auth.Application.Interfaces;
using Tekram.Api.src.auth.Domain;
using Tekram.Api.src.shared;

public class OtpRepository(TekramDbContext db) : IOtpRepository
{
    public async Task<OtpCode?> GetLatestActiveCodeAsync(Guid userId, string channel,
        CancellationToken ct = default)
    {
        return await db.OtpCodes
            .Where(o => o.UserId == userId && o.Channel == channel && o.ConsumedAt == null)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task AddAsync(OtpCode otpCode, CancellationToken ct = default)
    {
        await db.OtpCodes.AddAsync(otpCode, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task ConsumeAsync(Guid otpId, CancellationToken ct = default)
    {
        var otp = await db.OtpCodes.FirstOrDefaultAsync(o => o.Id == otpId, ct);
        if (otp is not null)
        {
            otp.ConsumedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<int> CountRecentResendsAsync(Guid userId, string channel, TimeSpan window,
        CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.Subtract(window);
        return await db.OtpCodes
            .CountAsync(o => o.UserId == userId && o.Channel == channel && o.CreatedAt >= since, ct);
    }
}
