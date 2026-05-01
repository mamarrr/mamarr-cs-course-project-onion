using WebApp.UI.Workspace;

namespace WebApp.UI.Breadcrumbs;

public class BreadcrumbBuilder : IBreadcrumbBuilder
{
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
                    : $"/m/{workspace.ManagementCompanySlug}",
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
                    : $"/m/{workspace.ManagementCompanySlug}/c/{workspace.CustomerSlug}",
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
                    : $"/m/{workspace.ManagementCompanySlug}/c/{workspace.CustomerSlug}/p/{workspace.PropertySlug}",
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
                    : $"/m/{workspace.ManagementCompanySlug}/c/{workspace.CustomerSlug}/p/{workspace.PropertySlug}/u/{workspace.UnitSlug}",
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
                    : $"/m/{workspace.ManagementCompanySlug}/r/{workspace.ResidentIdCode}",
                IsCurrent = workspace.Level == WorkspaceLevel.Resident,
                Level = WorkspaceLevel.Resident
            });
        }

        return links;
    }
}
