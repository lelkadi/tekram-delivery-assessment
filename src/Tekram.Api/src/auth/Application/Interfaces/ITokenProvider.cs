using Tekram.Api.src.auth.Domain;

namespace Tekram.Api.src.auth.Application.Interfaces;

public interface ITokenProvider
{
    string GenerateToken(User user);
    TimeSpan TokenExpiration { get; }
}
