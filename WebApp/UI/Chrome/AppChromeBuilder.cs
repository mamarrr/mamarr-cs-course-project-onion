using System.Globalization;
using App.Resources.Views;
using WebApp.UI.Breadcrumbs;
using WebApp.UI.Culture;
using WebApp.UI.Navigation;
using WebApp.UI.UserMenu;
using WebApp.UI.Workspace;

namespace WebApp.UI.Chrome;

public sealed class AppChromeBuilder : IAppChromeBuilder
{
    private readonly IWorkspaceResolver _workspaceResolver;
    private readonly IBreadcrumbBuilder _breadcrumbBuilder;
    private readonly INavigationBuilder _navigationBuilder;
    private readonly ICultureOptionsBuilder _cultureOptionsBuilder;
    private readonly IUserMenuBuilder _userMenuBuilder;

    public AppChromeBuilder(
        IWorkspaceResolver workspaceResolver,
        IBreadcrumbBuilder breadcrumbBuilder,
        INavigationBuilder navigationBuilder,
        ICultureOptionsBuilder cultureOptionsBuilder,
        IUserMenuBuilder userMenuBuilder)
    {
        _workspaceResolver = workspaceResolver;
        _breadcrumbBuilder = breadcrumbBuilder;
        _navigationBuilder = navigationBuilder;
        _cultureOptionsBuilder = cultureOptionsBuilder;
        _userMenuBuilder = userMenuBuilder;
    }

    public async Task<AppChromeViewModel> BuildAsync(
        AppChromeRequest request,
        CancellationToken cancellationToken = default)
    {
        var resolvedWorkspace = await _workspaceResolver.ResolveAsync(request, cancellationToken);
        var currentUiCultureName = CultureInfo.CurrentUICulture.Name;

        return new AppChromeViewModel
        {
            PageTitle = request.PageTitle,
            WorkspaceEyebrow = GetWorkspaceEyebrow(resolvedWorkspace.Workspace.Level),
            ActiveSection = request.ActiveSection,
            Workspace = resolvedWorkspace.Workspace,
            Breadcrumbs = _breadcrumbBuilder.Build(resolvedWorkspace.Workspace),
            NavigationItems = _navigationBuilder.Build(
                resolvedWorkspace.Workspace,
                request.ActiveSection,
                resolvedWorkspace.CanManageCompanyUsers),
            ManagementWorkspaceOptions = resolvedWorkspace.ManagementWorkspaceOptions,
            CustomerWorkspaceOptions = resolvedWorkspace.CustomerWorkspaceOptions,
            CultureOptions = _cultureOptionsBuilder.Build(currentUiCultureName),
            UserMenu = _userMenuBuilder.Build(request.User),
            CurrentPathAndQuery = $"{request.HttpContext.Request.Path}{request.HttpContext.Request.QueryString}",
            CurrentUiCultureName = currentUiCultureName
        };
    }

    private static string GetWorkspaceEyebrow(WorkspaceLevel level)
    {
        return level switch
        {
            WorkspaceLevel.Customer => T("CustomerWorkspace", "Customer workspace"),
            WorkspaceLevel.Property => T("PropertyWorkspace", "Property workspace"),
            WorkspaceLevel.Unit => T("UnitWorkspace", "Unit workspace"),
            WorkspaceLevel.Resident => T("ResidentWorkspace", "Resident workspace"),
            WorkspaceLevel.ManagementCompany => UiText.ManagementArea,
            _ => UiText.ManagementWorkspace
        };
    }

    private static string T(string key, string fallback)
    {
        return UiText.ResourceManager.GetString(key) ?? fallback;
    }
}
