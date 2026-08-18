using Behsazan.Application.DTOs;
using Behsazan.Application.Interfaces;
using Behsazan.Domain.Entities;
using Behsazan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Behsazan.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;

    public AuthService(
        IDbContextFactory<AppDbContext> dbContextFactory,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IConfiguration configuration)
    {
        _dbContextFactory = dbContextFactory;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        #region Find User
        var user = await dbContext.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user is null || !user.IsActive || user.IsDeleted)
            return null;
        #endregion

        #region Verify Password
        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            return null;
        #endregion

        #region Build Response
        var roles = user.UserRoles
            .Select(ur => ur.Role.Name)
            .ToList();

        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Key)
            .Distinct()
            .ToList();

        var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationInMinutes"] ?? "480");

        var loginResponse = new LoginResponseDto
        {
            UserId = user.Id,
            Username = user.Username,
            Expiration = DateTime.UtcNow.AddMinutes(expirationMinutes),
            Roles = roles,
            Permissions = permissions,
        };

        loginResponse.Token = _jwtTokenService.GenerateToken(loginResponse);
        return loginResponse;
        #endregion
    }

    public async Task<bool> HasPermissionAsync(int userId, string permissionKey)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        return await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && u.IsActive)
            .SelectMany(u => u.UserRoles)
            .SelectMany(ur => ur.Role.RolePermissions)
            .AnyAsync(rp => rp.Permission.Key == permissionKey);
    }
}
