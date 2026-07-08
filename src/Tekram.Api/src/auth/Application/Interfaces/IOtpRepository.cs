using Tekram.Api.src.auth.Domain;

namespace Tekram.Api.src.auth.Application.Interfaces;

public interface IOtpRepository
{
    Task<OtpCode?> GetLatestActiveCodeAsync(Guid userId, string channel, CancellationToken ct = default);
    Task AddAsync(OtpCode otpCode, CancellationToken ct = default);
    Task ConsumeAsync(Guid otpId, CancellationToken ct = default);
    Task<int> CountRecentResendsAsync(Guid userId, string channel, TimeSpan within, CancellationToken ct = default);
}
