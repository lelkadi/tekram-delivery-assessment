namespace Tekram.Api.src.auth.Application.Interfaces;

public interface INotificationGateway
{
    Task SendOtpAsync(string? email, string? phone, string channel, string code, CancellationToken ct = default);
}
