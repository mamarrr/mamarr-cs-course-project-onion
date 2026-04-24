using System.Security.Claims;

namespace WebApp.UI.UserMenu;

public sealed class UserMenuBuilder : IUserMenuBuilder
{
    public UserMenuViewModel Build(ClaimsPrincipal user)
    {
        return new UserMenuViewModel
        {
            IsAuthenticated = user.Identity?.IsAuthenticated == true,
            DisplayName = user.Identity?.Name ?? string.Empty,
            Email = user.FindFirstValue(ClaimTypes.Email)
        };
    }
}
