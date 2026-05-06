using WebApp.UI.Breadcrumbs;
using WebApp.UI.Culture;
using WebApp.UI.Navigation;
using WebApp.UI.UserMenu;
using WebApp.UI.Workspace;

namespace WebApp.UI.Chrome;

public class AppChromeViewModel
{
    public string PageTitle { get; init; } = string.Empty;

    public string WorkspaceEyebrow { get; init; } = string.Empty;

    public string ActiveSection { get; init; } = string.Empty;

    public WorkspaceIdentityViewModel Workspace { get; init; } = new();

    public IReadOnlyList<BreadcrumbLinkViewModel> Breadcrumbs { get; init; } = [];

    public IReadOnlyList<NavigationItemViewModel> NavigationItems { get; init; } = [];

    public IReadOnlyList<WorkspaceSwitchOptionViewModel> ManagementWorkspaceOptions { get; init; } = [];

    public IReadOnlyList<WorkspaceSwitchOptionViewModel> CustomerWorkspaceOptions { get; init; } = [];

    public WorkspaceSwitchOptionViewModel? ResidentWorkspaceOption { get; init; }

    public IReadOnlyList<CultureOptionViewModel> CultureOptions { get; init; } = [];

    public UserMenuViewModel UserMenu { get; init; } = new();

    public string CurrentPathAndQuery { get; init; } = string.Empty;

    public string CurrentUiCultureName { get; init; } = string.Empty;
}
