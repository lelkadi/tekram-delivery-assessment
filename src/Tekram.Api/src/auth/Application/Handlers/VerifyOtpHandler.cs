namespace Tekram.Api.src.auth.Application.Handlers;

using FluentValidation;
using Tekram.Api.src.auth.Application.DTOs;
using Tekram.Api.src.auth.Application.Interfaces;
using Tekram.Api.src.shared;

public class VerifyOtpHandler(
    IUserRepository userRepository,
    IOtpRepository otpRepository,
    IPasswordHasher passwordHasher,
    IValidator<VerifyOtpRequest> validator)
{
    public async Task<OtpVerifyResponse> HandleAsync(Guid userId, string channel, VerifyOtpRequest request,
        CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(request, ct);

        var user = await userRepository.GetByIdAsync(userId, ct)
                   ?? throw new DomainException(404, ErrorCodes.InvalidCredentials, "User not found.");

        var otpCode = await otpRepository.GetLatestActiveCodeAsync(userId, channel, ct);

        if (otpCode is null || otpCode.ExpiresAt < DateTime.UtcNow)
            throw new DomainException(422, ErrorCodes.InvalidOrExpiredCode,
                "Invalid or expired verification code.");

        if (!passwordHasher.Verify(request.Code, otpCode.CodeHash))
            throw new DomainException(422, ErrorCodes.InvalidOrExpiredCode,
                "Invalid or expired verification code.");

        await otpRepository.ConsumeAsync(otpCode.Id, ct);

        if (channel == "email")
            user.EmailVerified = true;
        else if (channel == "phone")
            user.PhoneVerified = true;

        await userRepository.UpdateAsync(user, ct);

        return new OtpVerifyResponse(
            Channel: channel,
            EmailVerified: user.EmailVerified,
            PhoneVerified: user.PhoneVerified,
            FullyVerified: user.EmailVerified && user.PhoneVerified
        );
    }
}
