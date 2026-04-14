using System.Security.Claims;
using App.BLL.Onboarding;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using WebApp.ViewModels.Management.Layout;

namespace WebApp.Services.ManagementLayout;

public class ManagementLayoutViewModelProvider : IManagementLayoutViewModelProvider
{
    private readonly IUserContextCatalogService _userContextCatalogService;
    private readonly IOptions<RequestLocalizationOptions> _localizationOptions;

    public ManagementLayoutViewModelProvider(
        IUserContextCatalogService userContextCatalogService,
        IOptions<RequestLocalizationOptions> localizationOptions)
    {
        _userContextCatalogService = userContextCatalogService;
        _localizationOptions = localizationOptions;
    }

    public async Task<ManagementLayoutViewModel> BuildAsync(
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
            .Select(c => new ManagementLayoutCultureOptionViewModel
            {
                Value = c.Name,
                Text = c.NativeName,
                IsCurrent = currentUiCultureName == c.Name
            })
            .ToList();

        return new ManagementLayoutViewModel
        {
            CurrentController = currentController,
            CompanySlug = companySlug,
            ManagementCompanyName = contextCatalog.ManagementCompanyName,
            CanManageCompanyUsers = contextCatalog.CanManageCompanyUsers,
            HasResidentContext = contextCatalog.HasResidentContext,
            CurrentPathAndQuery = currentPathAndQuery,
            CurrentUiCultureName = currentUiCultureName,
            ManagementContexts = contextCatalog.ManagementCompanies
                .Select(x => new ManagementLayoutContextOptionViewModel
                {
                    Id = x.ManagementCompanyId,
                    Slug = x.Slug,
                    Name = x.CompanyName
                })
                .ToList(),
            CustomerContexts = contextCatalog.Customers
                .Select(x => new ManagementLayoutContextOptionViewModel
                {
                    Id = x.CustomerId,
                    Name = x.Name
                })
                .ToList(),
            CultureOptions = cultureOptions
        };
    }

    private static ManagementLayoutViewModel BuildEmpty(
        string currentController,
        string companySlug,
        string currentPathAndQuery,
        string currentUiCultureName)
    {
        return new ManagementLayoutViewModel
        {
            CurrentController = currentController,
            CompanySlug = companySlug,
            CurrentPathAndQuery = currentPathAndQuery,
            CurrentUiCultureName = currentUiCultureName
        };
    }
}
