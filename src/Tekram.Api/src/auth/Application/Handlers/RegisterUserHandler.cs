namespace Tekram.Api.src.auth.Application.Handlers;

using FluentValidation;
using Tekram.Api.src.auth.Application.DTOs;
using Tekram.Api.src.auth.Application.Interfaces;
using Tekram.Api.src.auth.Domain;
using Tekram.Api.src.shared;

public class RegisterUserHandler(
    IUserRepository userRepository,
    IOtpRepository otpRepository,
    IPasswordHasher passwordHasher,
    ITokenProvider tokenProvider,
    INotificationGateway notificationGateway,
    IValidator<RegisterRequest> validator)
{
    public async Task<AuthResponse> HandleAsync(RegisterRequest request, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(request, ct);

        if (await userRepository.EmailExistsAsync(request.Email, ct))
            throw new DomainException(409, ErrorCodes.EmailAlreadyExists, "Email is already registered.");

        if (await userRepository.PhoneExistsAsync(request.Phone, ct))
            throw new DomainException(409, ErrorCodes.PhoneAlreadyExists, "Phone is already registered.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email.ToLowerInvariant(),
            Phone = request.Phone,
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = request.Role,
            EmailVerified = false,
            PhoneVerified = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await userRepository.AddAsync(user, ct);

        var emailCode = GenerateOtpCode();
        var phoneCode = GenerateOtpCode();
        var expiresAt = DateTime.UtcNow.AddMinutes(10);

        var emailOtp = new OtpCode
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Channel = "email",
            CodeHash = passwordHasher.Hash(emailCode),
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };

        var phoneOtp = new OtpCode
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Channel = "phone",
            CodeHash = passwordHasher.Hash(phoneCode),
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };

        await otpRepository.AddAsync(emailOtp, ct);
        await otpRepository.AddAsync(phoneOtp, ct);

        await notificationGateway.SendOtpAsync(user.Email, user.Phone, "email", emailCode, ct);
        await notificationGateway.SendOtpAsync(user.Email, user.Phone, "phone", phoneCode, ct);

        var token = tokenProvider.GenerateToken(user);

        return new AuthResponse(
            Id: user.Id,
            Name: user.Name,
            Email: user.Email,
            Phone: user.Phone,
            Role: user.Role,
            Token: token,
            TokenExpiresAt: tokenProvider.TokenExpiration
        );
    }

    private static string GenerateOtpCode()
    {
        return Random.Shared.Next(100000, 999999).ToString();
    }
}
