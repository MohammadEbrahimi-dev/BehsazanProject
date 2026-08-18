using Behsazan.Application.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;

namespace Behsazan.Presentation.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private const string AuthKey = "authUser";
    private readonly ProtectedLocalStorage _protectedLocalStorage;
    private readonly TokenAccessor _tokenAccessor;
    private readonly NavigationManager _navManager;
    private readonly ILogger<CustomAuthenticationStateProvider> _logger;

    private AuthenticationState? _cachedState;
    private DateTime? _cachedExpirationUtc;

    public CustomAuthenticationStateProvider(
        ProtectedLocalStorage protectedLocalStorage,
        TokenAccessor tokenAccessor,
        NavigationManager navManager,
        ILogger<CustomAuthenticationStateProvider> logger)
    {
        _protectedLocalStorage = protectedLocalStorage;
        _tokenAccessor = tokenAccessor;
        _navManager = navManager;
        _logger = logger;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_cachedState is not null)
        {
            if (_cachedExpirationUtc is null || _cachedExpirationUtc > DateTime.UtcNow)
                return _cachedState;

            _logger.LogInformation("Cached auth token expired at {Exp}. Clearing storage & redirecting to login.", _cachedExpirationUtc);
            await ClearPersistedAuthAsync();
            RedirectToLogin();
            return CacheAnonymous();
        }

        try
        {
            var result = await _protectedLocalStorage.GetAsync<LoginResponseDto>(AuthKey);
            var savedUser = result.Success ? result.Value : null;

            if (savedUser is null)
            {
                _tokenAccessor.Clear();
                return CacheAnonymous();
            }

            if (savedUser.Expiration <= DateTime.UtcNow)
            {
                _logger.LogInformation("Stored auth token expired at {Exp}. Clearing storage & redirecting to login.", savedUser.Expiration);
                await ClearPersistedAuthAsync();
                RedirectToLogin();
                return CacheAnonymous();
            }

            if (!string.IsNullOrWhiteSpace(savedUser.Token))
            {
                _tokenAccessor.SetToken(savedUser.Token);
            }

            return CacheAuthenticated(savedUser);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to restore authentication state from storage. Treating as anonymous.");
            _tokenAccessor.Clear();
            return CacheAnonymous();
        }
    }

    public async Task LoginAsync(LoginResponseDto user)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));

        await _protectedLocalStorage.SetAsync(AuthKey, user);
        if (!string.IsNullOrWhiteSpace(user.Token))
        {
            _tokenAccessor.SetToken(user.Token);
        }

        var state = CacheAuthenticated(user);
        NotifyAuthenticationStateChanged(Task.FromResult(state));
    }

    public async Task LogoutAsync()
    {
        await ClearPersistedAuthAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(CacheAnonymous()));
    }

    public async Task<LoginResponseDto?> GetCurrentUserAsync()
    {
        try
        {
            if (_cachedState?.User.Identity?.IsAuthenticated == true
                && (_cachedExpirationUtc is null || _cachedExpirationUtc > DateTime.UtcNow))
            {
                var user = _cachedState.User;
                return new LoginResponseDto
                {
                    UserId = int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0,
                    Username = user.Identity?.Name ?? string.Empty,
                    Expiration = _cachedExpirationUtc ?? DateTime.UtcNow,
                    Roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList(),
                    Permissions = user.FindAll("Permission").Select(c => c.Value).ToList(),
                    Token = _tokenAccessor.CurrentToken ?? string.Empty
                };
            }

            var result = await _protectedLocalStorage.GetAsync<LoginResponseDto>(AuthKey);
            var savedUser = result.Success ? result.Value : null;
            if (savedUser is not null && savedUser.Expiration <= DateTime.UtcNow)
            {
                await LogoutAsync();
                RedirectToLogin();
                return null;
            }

            if (savedUser is not null)
                CacheAuthenticated(savedUser);

            return savedUser;
        }
        catch
        {
            return null;
        }
    }

    private AuthenticationState CacheAuthenticated(LoginResponseDto savedUser)
    {
        var state = BuildAuthenticatedState(savedUser);
        _cachedState = state;
        _cachedExpirationUtc = savedUser.Expiration;
        return state;
    }

    private AuthenticationState CacheAnonymous()
    {
        var state = Anonymous();
        _cachedState = state;
        _cachedExpirationUtc = null;
        return state;
    }

    private async Task ClearPersistedAuthAsync()
    {
        try
        {
            await _protectedLocalStorage.DeleteAsync(AuthKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete persisted auth storage entry.");
        }

        _tokenAccessor.Clear();
        _cachedState = null;
        _cachedExpirationUtc = null;
    }

    private static AuthenticationState BuildAuthenticatedState(LoginResponseDto savedUser)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, savedUser.UserId.ToString()),
            new(ClaimTypes.Name, savedUser.Username),
        };

        foreach (var role in savedUser.Roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        foreach (var permission in savedUser.Permissions)
            claims.Add(new Claim("Permission", permission));

        var identity = new ClaimsIdentity(claims, "CustomAuth");
        var user = new ClaimsPrincipal(identity);
        return new AuthenticationState(user);
    }

    private static AuthenticationState Anonymous() => new(new ClaimsPrincipal(new ClaimsIdentity()));

    private void RedirectToLogin()
    {
        try
        {
            _navManager.NavigateTo("/login", forceLoad: false);
        }
        catch
        {
        }
    }
}
