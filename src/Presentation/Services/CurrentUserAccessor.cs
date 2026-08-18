using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Behsazan.Presentation.Services;

public class CurrentUserAccessor
{
    private readonly AuthenticationStateProvider _authStateProvider;

    public CurrentUserAccessor(AuthenticationStateProvider authStateProvider)
    {
        _authStateProvider = authStateProvider;
    }

    public async Task<int> GetUserIdAsync()
    {
        var state = await _authStateProvider.GetAuthenticationStateAsync();
        var claim = state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return int.TryParse(claim, out var userId) ? userId : 0;
    }
}
