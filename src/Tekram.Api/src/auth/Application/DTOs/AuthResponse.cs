namespace Tekram.Api.src.auth.Application.DTOs;

public record AuthResponse(
    Guid Id,
    string Name,
    string Email,
    string Phone,
    string Role,
    string Token,
    DateTime TokenExpiresAt
);
