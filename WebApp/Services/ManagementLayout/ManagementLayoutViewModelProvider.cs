using System.Security.Claims;
using App.BLL.Onboarding;
using WebApp.Services.SharedLayout;
using WebApp.ViewModels.Management.Layout;

namespace WebApp.Services.ManagementLayout;

public class ManagementLayoutViewModelProvider : IManagementLayoutViewModelProvider
{
    private readonly IWorkspaceLayoutContextProvider _workspaceLayoutContextProvider;
    private readonly IUserContextCatalogService _userContextCatalogService;

    public ManagementLayoutViewModelProvider(
        IWorkspaceLayoutContextProvider workspaceLayoutContextProvider,
        IUserContextCatalogService userContextCatalogService)
    {
        _workspaceLayoutContextProvider = workspaceLayoutContextProvider;
        _userContextCatalogService = userContextCatalogService;
    }

    public async Task<ManagementLayoutViewModel> BuildAsync(
        ClaimsPrincipal user,
        string currentController,
        string companySlug,
        string currentPathAndQuery,
        string currentUiCultureName,
        CancellationToken cancellationToken = default)
    {
        var sharedContext = await _workspaceLayoutContextProvider.BuildAsync(
            user,
            currentController,
            companySlug,
            currentPathAndQuery,
            currentUiCultureName,
            cancellationToken);

        var canManageCompanyUsers = false;
        var appUserIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(appUserIdValue, out var appUserId))
        {
            var contextCatalog = await _userContextCatalogService.GetUserContextCatalogAsync(appUserId, companySlug, cancellationToken);
            canManageCompanyUsers = contextCatalog.CanManageCompanyUsers;
        }

        return new ManagementLayoutViewModel
        {
            CurrentController = sharedContext.CurrentController,
            CompanySlug = sharedContext.CompanySlug,
            ManagementCompanyName = sharedContext.WorkspaceName,
            CanManageCompanyUsers = canManageCompanyUsers,
            HasResidentContext = sharedContext.HasResidentContext,
            CurrentPathAndQuery = sharedContext.CurrentPathAndQuery,
            CurrentUiCultureName = sharedContext.CurrentUiCultureName,
            ManagementContexts = sharedContext.ManagementContexts
                .Select(x => new ManagementLayoutContextOptionViewModel
                {
                    Id = x.Id,
                    Slug = x.Slug,
                    Name = x.Name
                })
                .ToList(),
            CustomerContexts = sharedContext.CustomerContexts
                .Select(x => new ManagementLayoutContextOptionViewModel
                {
                    Id = x.Id,
                    Slug = x.Slug,
                    Name = x.Name
                })
                .ToList(),
            CultureOptions = sharedContext.CultureOptions
                .Select(x => new ManagementLayoutCultureOptionViewModel
                {
                    Value = x.Value,
                    Text = x.Text,
                    IsCurrent = x.IsCurrent
                })
                .ToList()
        };
    }
}
