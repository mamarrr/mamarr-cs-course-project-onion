namespace WebApp.UI.UserMenu;

public class UserMenuViewModel
{
    public string DisplayName { get; init; } = string.Empty;

    public string? Email { get; init; }

    public bool IsAuthenticated { get; init; }
}
