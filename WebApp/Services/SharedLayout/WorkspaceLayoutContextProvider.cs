using System.Security.Claims;
using App.BLL.Onboarding;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using WebApp.ViewModels.Shared.Layout;

namespace WebApp.Services.SharedLayout;

public class WorkspaceLayoutContextProvider : IWorkspaceLayoutContextProvider
{
    private readonly IUserContextCatalogService _userContextCatalogService;
    private readonly IOptions<RequestLocalizationOptions> _localizationOptions;

    public WorkspaceLayoutContextProvider(
        IUserContextCatalogService userContextCatalogService,
        IOptions<RequestLocalizationOptions> localizationOptions)
    {
        _userContextCatalogService = userContextCatalogService;
        _localizationOptions = localizationOptions;
    }

    public async Task<WorkspaceLayoutContextViewModel> BuildAsync(
        ClaimsPrincipal user,
        string currentController,
        string companySlug,
        string currentPathAndQuery,
        string currentUiCultureName,
        CancellationToken cancellationToken = default)
    {
        var appUserIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(appUserIdValue, out var appUserId))
        {
            return BuildEmpty(currentController, companySlug, currentPathAndQuery, currentUiCultureName);
        }

        var contextCatalog = await _userContextCatalogService.GetUserContextCatalogAsync(appUserId, companySlug, cancellationToken);

        var cultureOptions = _localizationOptions.Value.SupportedUICultures!
            .Select(c => new WorkspaceLayoutCultureOptionViewModel
            {
                Value = c.Name,
                Text = c.NativeName,
                IsCurrent = currentUiCultureName == c.Name
            })
            .ToList();

        return new WorkspaceLayoutContextViewModel
        {
            CurrentController = currentController,
            CompanySlug = companySlug,
            WorkspaceName = contextCatalog.ManagementCompanyName,
            HasResidentContext = contextCatalog.HasResidentContext,
            CurrentPathAndQuery = currentPathAndQuery,
            CurrentUiCultureName = currentUiCultureName,
            ManagementContexts = contextCatalog.ManagementCompanies
                .Select(x => new WorkspaceLayoutContextOptionViewModel
                {
                    Id = x.ManagementCompanyId,
                    Slug = x.Slug,
                    Name = x.CompanyName
                })
                .ToList(),
            CustomerContexts = contextCatalog.Customers
                .Select(x => new WorkspaceLayoutContextOptionViewModel
                {
                    Id = x.CustomerId,
                    Name = x.Name
                })
                .ToList(),
            CultureOptions = cultureOptions
        };
    }

    private static WorkspaceLayoutContextViewModel BuildEmpty(
        string currentController,
        string companySlug,
        string currentPathAndQuery,
        string currentUiCultureName)
    {
        return new WorkspaceLayoutContextViewModel
        {
            CurrentController = currentController,
            CompanySlug = companySlug,
            CurrentPathAndQuery = currentPathAndQuery,
            CurrentUiCultureName = currentUiCultureName
        };
    }
}
