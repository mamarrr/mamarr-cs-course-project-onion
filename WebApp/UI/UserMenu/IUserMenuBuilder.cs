using System.Security.Claims;

namespace WebApp.UI.UserMenu;

public interface IUserMenuBuilder
{
    UserMenuViewModel Build(ClaimsPrincipal user);
}
