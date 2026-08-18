using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Behsazan.Application.DTOs;
using Behsazan.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Behsazan.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(LoginResponseDto loginResponse)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key is not configured.");
        var issuer = _configuration["Jwt:Issuer"] ?? "Behsazan";
        var audience = _configuration["Jwt:Audience"] ?? "Behsazan";
        var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationInMinutes"] ?? "480");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, loginResponse.UserId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, loginResponse.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        #region Role Claims
        foreach (var role in loginResponse.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }
        #endregion

        #region Permission Claims
        foreach (var permission in loginResponse.Permissions)
        {
            claims.Add(new Claim("Permission", permission));
        }
        #endregion

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public UserSessionDto? GetUserSessionFromToken(string token)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key is not configured.");
        var issuer = _configuration["Jwt:Issuer"] ?? "Behsazan";
        var audience = _configuration["Jwt:Audience"] ?? "Behsazan";

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ClockSkew = TimeSpan.Zero,
        };

        var handler = new JwtSecurityTokenHandler();

        try
        {
            var principal = handler.ValidateToken(token, parameters, out _);

            var userId = int.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? "0");
            var username = principal.FindFirstValue(JwtRegisteredClaimNames.UniqueName) ?? string.Empty;

            var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            var permissions = principal.FindAll("Permission").Select(c => c.Value).ToList();

            return new UserSessionDto
            {
                UserId = userId,
                Username = username,
                Roles = roles,
                Permissions = permissions,
            };
        }
        catch
        {
            return null;
        }
    }
}
