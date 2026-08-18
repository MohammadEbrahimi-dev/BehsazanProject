using Behsazan.Application.DTOs;

namespace Behsazan.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto request);

    Task<bool> HasPermissionAsync(int userId, string permissionKey);
}
