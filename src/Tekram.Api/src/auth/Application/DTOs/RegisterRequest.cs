namespace Tekram.Api.src.auth.Application.DTOs;

public record RegisterRequest(
    string Name,
    string Email,
    string Phone,
    string Password,
    string Role
);
