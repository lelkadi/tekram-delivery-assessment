namespace Tekram.Api.src.auth.Infrastructure;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Tekram.Api.src.auth.Application.Interfaces;
using Tekram.Api.src.auth.Domain;

public class JwtTokenProvider(IConfiguration configuration) : ITokenProvider
{
    private readonly string _secret = configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException("Jwt:Secret is not configured");

    private readonly int _expirationMinutes = int.TryParse(configuration["Jwt:ExpirationMinutes"], out var mins)
        ? mins
        : 60;

    public DateTime TokenExpiration => DateTime.UtcNow.AddMinutes(_expirationMinutes);

    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("sub", user.Id.ToString()),
            new Claim("role", user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: TokenExpiration,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
