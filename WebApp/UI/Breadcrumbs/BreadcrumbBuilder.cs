using Microsoft.AspNetCore.Routing;
using WebApp.UI.Routing;
using WebApp.UI.Workspace;

namespace WebApp.UI.Breadcrumbs;

public class BreadcrumbBuilder : IBreadcrumbBuilder
{
    private readonly LinkGenerator _linkGenerator;

    public BreadcrumbBuilder(LinkGenerator linkGenerator)
    {
        _linkGenerator = linkGenerator;
    }

    public IReadOnlyList<BreadcrumbLinkViewModel> Build(WorkspaceIdentityViewModel workspace)
    {
        var links = new List<BreadcrumbLinkViewModel>();

        if (!string.IsNullOrWhiteSpace(workspace.ManagementCompanySlug))
        {
            links.Add(new BreadcrumbLinkViewModel
            {
                Label = workspace.ManagementCompanyName ?? workspace.ManagementCompanySlug,
                Url = workspace.Level == WorkspaceLevel.ManagementCompany
                    ? null
                    : Route(PortalRouteNames.ManagementDashboard, new { companySlug = workspace.ManagementCompanySlug }),
                IsCurrent = workspace.Level == WorkspaceLevel.ManagementCompany,
                Level = WorkspaceLevel.ManagementCompany
            });
        }

        if (!string.IsNullOrWhiteSpace(workspace.CustomerSlug))
        {
            links.Add(new BreadcrumbLinkViewModel
            {
                Label = workspace.CustomerName ?? workspace.CustomerSlug,
                Url = workspace.Level == WorkspaceLevel.Customer
                    ? null
                    : Route(
                        PortalRouteNames.CustomerDashboard,
                        new { companySlug = workspace.ManagementCompanySlug, customerSlug = workspace.CustomerSlug }),
                IsCurrent = workspace.Level == WorkspaceLevel.Customer,
                Level = WorkspaceLevel.Customer
            });
        }

        if (!string.IsNullOrWhiteSpace(workspace.PropertySlug))
        {
            links.Add(new BreadcrumbLinkViewModel
            {
                Label = workspace.PropertyName ?? workspace.PropertySlug,
                Url = workspace.Level == WorkspaceLevel.Property
                    ? null
                    : Route(
                        PortalRouteNames.PropertyDashboard,
                        new
                        {
                            companySlug = workspace.ManagementCompanySlug,
                            customerSlug = workspace.CustomerSlug,
                            propertySlug = workspace.PropertySlug
                        }),
                IsCurrent = workspace.Level == WorkspaceLevel.Property,
                Level = WorkspaceLevel.Property
            });
        }

        if (!string.IsNullOrWhiteSpace(workspace.UnitSlug))
        {
            links.Add(new BreadcrumbLinkViewModel
            {
                Label = workspace.UnitName ?? workspace.UnitSlug,
                Url = workspace.Level == WorkspaceLevel.Unit
                    ? null
                    : Route(
                        PortalRouteNames.UnitDashboard,
                        new
                        {
                            companySlug = workspace.ManagementCompanySlug,
                            customerSlug = workspace.CustomerSlug,
                            propertySlug = workspace.PropertySlug,
                            unitSlug = workspace.UnitSlug
                        }),
                IsCurrent = workspace.Level == WorkspaceLevel.Unit,
                Level = WorkspaceLevel.Unit
            });
        }

        if (!string.IsNullOrWhiteSpace(workspace.ResidentIdCode))
        {
            links.Add(new BreadcrumbLinkViewModel
            {
                Label = workspace.ResidentDisplayName ?? workspace.ResidentIdCode,
                Url = workspace.Level == WorkspaceLevel.Resident
                    ? null
                    : Route(
                        PortalRouteNames.ResidentDashboard,
                        new { companySlug = workspace.ManagementCompanySlug, residentIdCode = workspace.ResidentIdCode }),
                IsCurrent = workspace.Level == WorkspaceLevel.Resident,
                Level = WorkspaceLevel.Resident
            });
        }

        return links;
    }

    private string Route(string routeName, object values)
    {
        return _linkGenerator.GetPathByName(routeName, values) ?? string.Empty;
    }
}
