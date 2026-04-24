using System.Security.Claims;
using App.BLL.Onboarding.WorkspaceCatalog;
using WebApp.UI.Chrome;

namespace WebApp.UI.Workspace;

public sealed class WorkspaceResolver : IWorkspaceResolver
{
    private readonly IUserWorkspaceCatalogService _userWorkspaceCatalogService;

    public WorkspaceResolver(IUserWorkspaceCatalogService userWorkspaceCatalogService)
    {
        _userWorkspaceCatalogService = userWorkspaceCatalogService;
    }

    public async Task<WorkspaceResolutionResult> ResolveAsync(
        AppChromeRequest request,
        CancellationToken cancellationToken = default)
    {
        var appUserIdValue = request.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(appUserIdValue, out var appUserId))
        {
            return new WorkspaceResolutionResult
            {
                Workspace = BuildFallbackWorkspace(request)
            };
        }

        var catalog = await _userWorkspaceCatalogService.GetUserContextCatalogAsync(
            appUserId,
            request.ManagementCompanySlug ?? string.Empty,
            cancellationToken);

        var managementOptions = catalog.ManagementCompanies
            .Select(x => new WorkspaceSwitchOptionViewModel
            {
                Id = x.ManagementCompanyId,
                Slug = x.Slug,
                Name = x.CompanyName,
                IsCurrent = string.Equals(x.Slug, request.ManagementCompanySlug, StringComparison.OrdinalIgnoreCase),
                Url = $"/m/{x.Slug}"
            })
            .ToList();

        var customerOptions = catalog.Customers
            .Select(x => new WorkspaceSwitchOptionViewModel
            {
                Id = x.CustomerId,
                Name = x.Name
            })
            .ToList();

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
                HasResidentContext = catalog.HasResidentContext
            },
            ManagementWorkspaceOptions = managementOptions,
            CustomerWorkspaceOptions = customerOptions,
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
}
