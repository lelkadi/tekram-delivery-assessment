namespace Tekram.Api.src.auth.Application.Handlers;

using FluentValidation;
using Tekram.Api.src.auth.Application.DTOs;
using Tekram.Api.src.auth.Application.Interfaces;
using Tekram.Api.src.shared;

public class LoginHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenProvider tokenProvider,
    IValidator<LoginRequest> validator)
{
    public async Task<AuthResponse> HandleAsync(LoginRequest request, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(request, ct);

        var user = await userRepository.GetByIdentifierAsync(request.Identifier, ct);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new DomainException(401, ErrorCodes.InvalidCredentials,
                "Invalid credentials.");

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
}
