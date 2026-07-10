namespace Tekram.Api.src.auth.Application.Handlers;

using FluentValidation;
using Tekram.Api.src.auth.Application.DTOs;
using Tekram.Api.src.auth.Application.Interfaces;
using Tekram.Api.src.auth.Domain;
using Tekram.Api.src.shared;

public class ResendOtpHandler(
    IUserRepository userRepository,
    IOtpRepository otpRepository,
    IPasswordHasher passwordHasher,
    INotificationGateway notificationGateway,
    IValidator<ResendOtpRequest> validator)
{
    private static readonly TimeSpan ResendWindow = TimeSpan.FromMinutes(15);
    private const int MaxResends = 3;

    public async Task HandleAsync(Guid userId, ResendOtpRequest request, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(request, ct);

        var user = await userRepository.GetByIdAsync(userId, ct)
                   ?? throw new DomainException(404, ErrorCodes.InvalidCredentials, "User not found.");

        var recentCount = await otpRepository.CountRecentResendsAsync(userId, request.Channel, ResendWindow, ct);

        if (recentCount >= MaxResends)
            throw new DomainException(429, ErrorCodes.OtpResendCooldown,
                "Too many resend attempts. Please wait 15 minutes.");

        var code = Random.Shared.Next(100000, 999999).ToString();
        var expiresAt = DateTime.UtcNow.AddMinutes(10);

        var otp = new OtpCode
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Channel = request.Channel,
            CodeHash = passwordHasher.Hash(code),
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };

        await otpRepository.AddAsync(otp, ct);

        await notificationGateway.SendOtpAsync(user.Email, user.Phone, request.Channel, code, ct);
    }
}
