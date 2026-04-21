using System.Security.Claims;
using App.BLL.Onboarding;
using App.BLL.Onboarding.WorkspaceCatalog;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using WebApp.ViewModels.Shared.Layout;

namespace WebApp.Services.SharedLayout;

public class WorkspaceLayoutContextProvider : IWorkspaceLayoutContextProvider
{
    private readonly IUserWorkspaceCatalogService _userWorkspaceCatalogService;
    private readonly IOptions<RequestLocalizationOptions> _localizationOptions;

    public WorkspaceLayoutContextProvider(
        IUserWorkspaceCatalogService userWorkspaceCatalogService,
        IOptions<RequestLocalizationOptions> localizationOptions)
    {
        _userWorkspaceCatalogService = userWorkspaceCatalogService;
        _localizationOptions = localizationOptions;
    }

    public async Task<WorkspaceLayoutContextViewModel> BuildAsync(
        ClaimsPrincipal user,
        WorkspaceLayoutRequestViewModel request,
        CancellationToken cancellationToken = default)
    {
        var appUserIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(appUserIdValue, out var appUserId))
        {
            return BuildEmpty(request);
        }

        var contextCatalog = await _userWorkspaceCatalogService.GetUserContextCatalogAsync(appUserId, request.CompanySlug, cancellationToken);

        var cultureOptions = _localizationOptions.Value.SupportedUICultures!
            .Select(c => new WorkspaceLayoutCultureOptionViewModel
            {
                Value = c.Name,
                Text = c.NativeName,
                IsCurrent = request.CurrentUiCultureName == c.Name
            })
            .ToList();

        return new WorkspaceLayoutContextViewModel
        {
            CurrentController = request.CurrentController,
            CompanySlug = request.CompanySlug,
            WorkspaceName = contextCatalog.ManagementCompanyName,
            HasResidentContext = contextCatalog.HasResidentContext,
            CurrentPathAndQuery = request.CurrentPathAndQuery,
            CurrentUiCultureName = request.CurrentUiCultureName,
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

    private static WorkspaceLayoutContextViewModel BuildEmpty(WorkspaceLayoutRequestViewModel request)
    {
        return new WorkspaceLayoutContextViewModel
        {
            CurrentController = request.CurrentController,
            CompanySlug = request.CompanySlug,
            CurrentPathAndQuery = request.CurrentPathAndQuery,
            CurrentUiCultureName = request.CurrentUiCultureName
        };
    }
}
