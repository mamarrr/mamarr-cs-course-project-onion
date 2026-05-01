namespace WebApp.UI.Navigation;

public class NavigationItemViewModel
{
    public string Label { get; init; } = string.Empty;

    public string Url { get; init; } = string.Empty;

    public string Section { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public bool IsVisible { get; init; } = true;

    public bool IsDisabled { get; init; }

    public string? IconCssClass { get; init; }
}
