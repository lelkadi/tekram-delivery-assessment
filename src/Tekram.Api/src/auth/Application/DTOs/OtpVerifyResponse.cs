namespace Tekram.Api.src.auth.Application.DTOs;

public record OtpVerifyResponse(
    string Channel,
    bool EmailVerified,
    bool PhoneVerified,
    bool FullyVerified
);
