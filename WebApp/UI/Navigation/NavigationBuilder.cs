using App.Resources.Views;
using Microsoft.AspNetCore.Routing;
using WebApp.UI.Routing;
using WebApp.UI.Workspace;

namespace WebApp.UI.Navigation;

public class NavigationBuilder : INavigationBuilder
{
    private readonly LinkGenerator _linkGenerator;

    public NavigationBuilder(LinkGenerator linkGenerator)
    {
        _linkGenerator = linkGenerator;
    }

    public IReadOnlyList<NavigationItemViewModel> Build(
        WorkspaceIdentityViewModel workspace,
        string activeSection,
        bool canManageCompanyUsers)
    {
        return workspace.Level switch
        {
            WorkspaceLevel.Customer => BuildCustomerNavigation(workspace, activeSection),
            WorkspaceLevel.Property => BuildPropertyNavigation(workspace, activeSection),
            WorkspaceLevel.Unit => BuildUnitNavigation(workspace, activeSection),
            WorkspaceLevel.Resident => BuildResidentNavigation(workspace, activeSection),
            _ => BuildManagementNavigation(workspace, activeSection, canManageCompanyUsers)
        };
    }

    private IReadOnlyList<NavigationItemViewModel> BuildManagementNavigation(
        WorkspaceIdentityViewModel workspace,
        string activeSection,
        bool canManageCompanyUsers)
    {
        var companySlug = workspace.ManagementCompanySlug ?? string.Empty;

        return new List<NavigationItemViewModel>
        {
            Link(UiText.Dashboard, Route(PortalRouteNames.ManagementDashboard, new { companySlug }), Sections.Dashboard, activeSection),
            Link(UiText.Profile, Route(PortalRouteNames.ManagementProfile, new { companySlug }), Sections.Profile, activeSection),
            Link(UiText.Tickets, Route(PortalRouteNames.ManagementTickets, new { companySlug }), Sections.Tickets, activeSection),
            Disabled(UiText.Properties, Sections.Properties, activeSection),
            Link(UiText.Customers, Route(PortalRouteNames.ManagementCustomers, new { companySlug }), Sections.Customers, activeSection),
            Link(UiText.Residents, Route(PortalRouteNames.ManagementResidents, new { companySlug }), Sections.Residents, activeSection),
            new()
            {
                Label = UiText.Users,
                Url = Route(PortalRouteNames.ManagementUsers, new { companySlug }),
                Section = Sections.CompanyUsers,
                IsVisible = canManageCompanyUsers,
                IsActive = activeSection == Sections.CompanyUsers
            },
            Link(UiText.Vendors, Route(PortalRouteNames.ManagementVendors, new { companySlug }), Sections.Vendors, activeSection)
        };
    }

    private IReadOnlyList<NavigationItemViewModel> BuildCustomerNavigation(
        WorkspaceIdentityViewModel workspace,
        string activeSection)
    {
        var companySlug = workspace.ManagementCompanySlug ?? string.Empty;
        var customerSlug = workspace.CustomerSlug ?? string.Empty;

        return new List<NavigationItemViewModel>
        {
            Link(UiText.Dashboard, Route(PortalRouteNames.CustomerDashboard, new { companySlug, customerSlug }), Sections.Dashboard, activeSection),
            Link(UiText.Profile, Route(PortalRouteNames.CustomerProfile, new { companySlug, customerSlug }), Sections.Profile, activeSection),
            Link(UiText.Tickets, Route(PortalRouteNames.CustomerTickets, new { companySlug, customerSlug }), Sections.Tickets, activeSection),
            Link(UiText.Properties, Route(PortalRouteNames.CustomerProperties, new { companySlug, customerSlug }), Sections.Properties, activeSection),
            Link(T("Residents", "Residents"), Route(PortalRouteNames.CustomerResidents, new { companySlug, customerSlug }), Sections.Residents, activeSection)
        };
    }

    private IReadOnlyList<NavigationItemViewModel> BuildPropertyNavigation(
        WorkspaceIdentityViewModel workspace,
        string activeSection)
    {
        var companySlug = workspace.ManagementCompanySlug ?? string.Empty;
        var customerSlug = workspace.CustomerSlug ?? string.Empty;
        var propertySlug = workspace.PropertySlug ?? string.Empty;

        return new List<NavigationItemViewModel>
        {
            Link(UiText.Dashboard, Route(PortalRouteNames.PropertyDashboard, new { companySlug, customerSlug, propertySlug }), Sections.Dashboard, activeSection),
            Link(UiText.Profile, Route(PortalRouteNames.PropertyProfile, new { companySlug, customerSlug, propertySlug }), Sections.Profile, activeSection),
            Link(T("Units", "Units"), Route(PortalRouteNames.PropertyUnits, new { companySlug, customerSlug, propertySlug }), Sections.Units, activeSection),
            Link(UiText.Residents, Route(PortalRouteNames.PropertyResidents, new { companySlug, customerSlug, propertySlug }), Sections.Residents, activeSection),
            Link(UiText.Tickets, Route(PortalRouteNames.PropertyTickets, new { companySlug, customerSlug, propertySlug }), Sections.Tickets, activeSection)
        };
    }

    private IReadOnlyList<NavigationItemViewModel> BuildUnitNavigation(
        WorkspaceIdentityViewModel workspace,
        string activeSection)
    {
        var companySlug = workspace.ManagementCompanySlug ?? string.Empty;
        var customerSlug = workspace.CustomerSlug ?? string.Empty;
        var propertySlug = workspace.PropertySlug ?? string.Empty;
        var unitSlug = workspace.UnitSlug ?? string.Empty;

        return new List<NavigationItemViewModel>
        {
            Link(UiText.Dashboard, Route(PortalRouteNames.UnitDashboard, new { companySlug, customerSlug, propertySlug, unitSlug }), Sections.Dashboard, activeSection),
            Link(UiText.Profile, Route(PortalRouteNames.UnitProfile, new { companySlug, customerSlug, propertySlug, unitSlug }), Sections.Profile, activeSection),
            Link(T("Tenants", "Tenants"), Route(PortalRouteNames.UnitTenants, new { companySlug, customerSlug, propertySlug, unitSlug }), Sections.Tenants, activeSection),
            Link(UiText.Tickets, Route(PortalRouteNames.UnitTickets, new { companySlug, customerSlug, propertySlug, unitSlug }), Sections.Tickets, activeSection)
        };
    }

    private IReadOnlyList<NavigationItemViewModel> BuildResidentNavigation(
        WorkspaceIdentityViewModel workspace,
        string activeSection)
    {
        var companySlug = workspace.ManagementCompanySlug ?? string.Empty;
        var residentIdCode = workspace.ResidentIdCode ?? string.Empty;

        return new List<NavigationItemViewModel>
        {
            Link(UiText.Dashboard, Route(PortalRouteNames.ResidentDashboard, new { companySlug, residentIdCode }), Sections.Dashboard, activeSection),
            Link(UiText.Profile, Route(PortalRouteNames.ResidentProfile, new { companySlug, residentIdCode }), Sections.Profile, activeSection),
            Link(T("Units", "Units"), Route(PortalRouteNames.ResidentUnits, new { companySlug, residentIdCode }), Sections.Units, activeSection),
            Link(UiText.Tickets, Route(PortalRouteNames.ResidentTickets, new { companySlug, residentIdCode }), Sections.Tickets, activeSection),
            Link(T("Representations", "Representations"), Route(PortalRouteNames.ResidentRepresentations, new { companySlug, residentIdCode }), Sections.Representations, activeSection),
            Link(T("Contacts", "Contacts"), Route(PortalRouteNames.ResidentContacts, new { companySlug, residentIdCode }), Sections.Contacts, activeSection)
        };
    }

    private string Route(string routeName, object values)
    {
        return _linkGenerator.GetPathByName(routeName, values) ?? string.Empty;
    }

    private static NavigationItemViewModel Link(string label, string url, string section, string activeSection)
    {
        return new NavigationItemViewModel
        {
            Label = label,
            Url = url,
            Section = section,
            IsActive = activeSection == section
        };
    }

    private static NavigationItemViewModel Disabled(string label, string section, string activeSection)
    {
        return new NavigationItemViewModel
        {
            Label = label,
            Section = section,
            IsDisabled = true,
            IsActive = activeSection == section
        };
    }

    private static string T(string key, string fallback)
    {
        return UiText.ResourceManager.GetString(key) ?? fallback;
    }
}
