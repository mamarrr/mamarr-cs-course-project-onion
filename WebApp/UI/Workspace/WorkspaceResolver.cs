using App.BLL.Contracts;
using App.BLL.DTO.Onboarding.Queries;
using Microsoft.AspNetCore.Routing;
using WebApp.UI.Chrome;
using WebApp.UI.PortalContext;
using WebApp.UI.Routing;

namespace WebApp.UI.Workspace;

public class WorkspaceResolver : IWorkspaceResolver
{
    private readonly IAppBLL _bll;
    private readonly ICurrentPortalContextResolver _portalContextResolver;
    private readonly LinkGenerator _linkGenerator;

    public WorkspaceResolver(
        IAppBLL bll,
        ICurrentPortalContextResolver portalContextResolver,
        LinkGenerator linkGenerator)
    {
        _bll = bll;
        _portalContextResolver = portalContextResolver;
        _linkGenerator = linkGenerator;
    }

    public async Task<WorkspaceResolutionResult> ResolveAsync(
        AppChromeRequest request,
        CancellationToken cancellationToken = default)
    {
        var portalContext = _portalContextResolver.Resolve();
        if (!portalContext.IsAuthenticated)
        {
            return new WorkspaceResolutionResult
            {
                Workspace = BuildFallbackWorkspace(request)
            };
        }

        var catalogResult = await _bll.WorkspaceCatalog.GetWorkspaceCatalogAsync(
            new GetWorkspaceCatalogQuery
            {
                AppUserId = portalContext.AppUserId!.Value,
                CompanySlug = request.ManagementCompanySlug ?? string.Empty
            },
            cancellationToken);
        var catalog = catalogResult.Value;

        var managementOptions = catalog.ManagementCompanies
            .Select(x => new WorkspaceSwitchOptionViewModel
            {
                Id = x.Id,
                Slug = x.Slug ?? string.Empty,
                Name = x.Name,
                IsCurrent = string.Equals(x.Slug, request.ManagementCompanySlug, StringComparison.OrdinalIgnoreCase),
                Url = Route(PortalRouteNames.ManagementDashboard, new { companySlug = x.Slug })
            })
            .ToList();

        var customerOptions = catalog.Customers
            .Select(x => new WorkspaceSwitchOptionViewModel
            {
                Id = x.Id,
                Slug = x.Slug ?? string.Empty,
                Name = x.Name,
                IsCurrent = string.Equals(x.Slug, request.CustomerSlug, StringComparison.OrdinalIgnoreCase),
                Url = !string.IsNullOrWhiteSpace(x.ManagementCompanySlug) && !string.IsNullOrWhiteSpace(x.Slug)
                    ? Route(
                        PortalRouteNames.CustomerDashboard,
                        new { companySlug = x.ManagementCompanySlug, customerSlug = x.Slug })
                    : null
            })
            .ToList();

        var residentOption = catalog.Resident is null
            ? null
            : new WorkspaceSwitchOptionViewModel
            {
                Id = catalog.Resident.Id,
                Slug = catalog.Resident.Slug ?? string.Empty,
                Name = catalog.Resident.Name,
                IsCurrent = string.Equals(catalog.Resident.Slug, request.ResidentIdCode, StringComparison.OrdinalIgnoreCase),
                Url = !string.IsNullOrWhiteSpace(catalog.Resident.ManagementCompanySlug)
                      && !string.IsNullOrWhiteSpace(catalog.Resident.Slug)
                    ? Route(
                        PortalRouteNames.ResidentDashboard,
                        new
                        {
                            companySlug = catalog.Resident.ManagementCompanySlug,
                            residentIdCode = catalog.Resident.Slug
                        })
                    : null
            };

        var managementCompanyName = request.ManagementCompanyName ?? catalog.ManagementCompanyName;
        var workspaceDisplayName = request.CurrentLevel switch
        {
            WorkspaceLevel.Customer => request.CustomerName ?? request.CustomerSlug ?? managementCompanyName,
            WorkspaceLevel.Property => request.PropertyName ?? request.PropertySlug ?? request.CustomerName ?? managementCompanyName,
            WorkspaceLevel.Unit => request.UnitName ?? request.UnitSlug ?? request.PropertyName ?? managementCompanyName,
            WorkspaceLevel.Resident => request.ResidentDisplayName ?? request.ResidentIdCode ?? managementCompanyName,
            _ => managementCompanyName
        };

        return new WorkspaceResolutionResult
        {
            Workspace = new WorkspaceIdentityViewModel
            {
                Level = request.CurrentLevel,
                ManagementCompanySlug = request.ManagementCompanySlug,
                ManagementCompanyName = managementCompanyName,
                CustomerSlug = request.CustomerSlug,
                CustomerName = request.CustomerName,
                PropertySlug = request.PropertySlug,
                PropertyName = request.PropertyName,
                UnitSlug = request.UnitSlug,
                UnitName = request.UnitName,
                ResidentIdCode = request.ResidentIdCode,
                ResidentDisplayName = request.ResidentDisplayName,
                ResidentSupportingText = request.ResidentSupportingText,
                DisplayName = workspaceDisplayName,
                HasResidentContext = residentOption is not null
            },
            ManagementWorkspaceOptions = managementOptions,
            CustomerWorkspaceOptions = customerOptions,
            ResidentWorkspaceOption = residentOption,
            CanManageCompanyUsers = catalog.CanManageCompanyUsers
        };
    }

    private static WorkspaceIdentityViewModel BuildFallbackWorkspace(AppChromeRequest request)
    {
        return new WorkspaceIdentityViewModel
        {
            Level = request.CurrentLevel,
            ManagementCompanySlug = request.ManagementCompanySlug,
            ManagementCompanyName = request.ManagementCompanyName,
            CustomerSlug = request.CustomerSlug,
            CustomerName = request.CustomerName,
            PropertySlug = request.PropertySlug,
            PropertyName = request.PropertyName,
            UnitSlug = request.UnitSlug,
            UnitName = request.UnitName,
            ResidentIdCode = request.ResidentIdCode,
            ResidentDisplayName = request.ResidentDisplayName,
            ResidentSupportingText = request.ResidentSupportingText,
            DisplayName = request.ResidentDisplayName
                ?? request.UnitName
                ?? request.PropertyName
                ?? request.CustomerName
                ?? request.ManagementCompanyName
                ?? string.Empty
        };
    }

    private string Route(string routeName, object values)
    {
        return _linkGenerator.GetPathByName(routeName, values) ?? string.Empty;
    }
}
