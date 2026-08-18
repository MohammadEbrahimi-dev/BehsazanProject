using Behsazan.Application.DTOs;

namespace Behsazan.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(LoginResponseDto loginResponse);

    UserSessionDto? GetUserSessionFromToken(string token);
}
