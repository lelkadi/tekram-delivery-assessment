namespace Tekram.Api.src.auth.Application.DTOs;

public record LoginRequest(
    string Identifier,
    string Password
);
