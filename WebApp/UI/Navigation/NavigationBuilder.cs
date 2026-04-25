using App.Resources.Views;
using WebApp.UI.Workspace;

namespace WebApp.UI.Navigation;

public sealed class NavigationBuilder : INavigationBuilder
{
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

    private static IReadOnlyList<NavigationItemViewModel> BuildManagementNavigation(
        WorkspaceIdentityViewModel workspace,
        string activeSection,
        bool canManageCompanyUsers)
    {
        var companySlug = workspace.ManagementCompanySlug ?? string.Empty;

        return new List<NavigationItemViewModel>
        {
            Link(UiText.Dashboard, $"/m/{companySlug}", Sections.Dashboard, activeSection),
            Link(UiText.Profile, $"/m/{companySlug}/profile", Sections.Profile, activeSection),
            Disabled(UiText.Tickets, Sections.Tickets, activeSection),
            Disabled(UiText.Properties, Sections.Properties, activeSection),
            Link(UiText.Customers, $"/m/{companySlug}/customers", Sections.Customers, activeSection),
            Link(UiText.Residents, $"/m/{companySlug}/residents", Sections.Residents, activeSection),
            new()
            {
                Label = UiText.Users,
                Url = $"/m/{companySlug}/users",
                Section = Sections.CompanyUsers,
                IsVisible = canManageCompanyUsers,
                IsActive = activeSection == Sections.CompanyUsers
            },
            Disabled(UiText.Vendors, Sections.Vendors, activeSection)
        };
    }

    private static IReadOnlyList<NavigationItemViewModel> BuildCustomerNavigation(
        WorkspaceIdentityViewModel workspace,
        string activeSection)
    {
        var companySlug = workspace.ManagementCompanySlug ?? string.Empty;
        var customerSlug = workspace.CustomerSlug ?? string.Empty;

        return new List<NavigationItemViewModel>
        {
            Link(UiText.Dashboard, $"/m/{companySlug}/c/{customerSlug}", Sections.Dashboard, activeSection),
            Link(UiText.Profile, $"/m/{companySlug}/c/{customerSlug}/profile", Sections.Profile, activeSection),
            Link(UiText.Tickets, $"/m/{companySlug}/c/{customerSlug}/tickets", Sections.Tickets, activeSection),
            Link(UiText.Properties, $"/m/{companySlug}/c/{customerSlug}/properties", Sections.Properties, activeSection),
            Link(T("Residents", "Residents"), $"/m/{companySlug}/c/{customerSlug}/residents", Sections.Residents, activeSection)
        };
    }

    private static IReadOnlyList<NavigationItemViewModel> BuildPropertyNavigation(
        WorkspaceIdentityViewModel workspace,
        string activeSection)
    {
        var companySlug = workspace.ManagementCompanySlug ?? string.Empty;
        var customerSlug = workspace.CustomerSlug ?? string.Empty;
        var propertySlug = workspace.PropertySlug ?? string.Empty;

        return new List<NavigationItemViewModel>
        {
            Link(UiText.Dashboard, $"/m/{companySlug}/c/{customerSlug}/p/{propertySlug}", Sections.Dashboard, activeSection),
            Link(UiText.Profile, $"/m/{companySlug}/c/{customerSlug}/p/{propertySlug}/profile", Sections.Profile, activeSection),
            Link(T("Units", "Units"), $"/m/{companySlug}/c/{customerSlug}/p/{propertySlug}/units", Sections.Units, activeSection),
            Link(UiText.Residents, $"/m/{companySlug}/c/{customerSlug}/p/{propertySlug}/residents", Sections.Residents, activeSection),
            Link(UiText.Tickets, $"/m/{companySlug}/c/{customerSlug}/p/{propertySlug}/tickets", Sections.Tickets, activeSection)
        };
    }

    private static IReadOnlyList<NavigationItemViewModel> BuildUnitNavigation(
        WorkspaceIdentityViewModel workspace,
        string activeSection)
    {
        var companySlug = workspace.ManagementCompanySlug ?? string.Empty;
        var customerSlug = workspace.CustomerSlug ?? string.Empty;
        var propertySlug = workspace.PropertySlug ?? string.Empty;
        var unitSlug = workspace.UnitSlug ?? string.Empty;

        return new List<NavigationItemViewModel>
        {
            Link(UiText.Dashboard, $"/m/{companySlug}/c/{customerSlug}/p/{propertySlug}/u/{unitSlug}", Sections.Dashboard, activeSection),
            Link(UiText.Profile, $"/m/{companySlug}/c/{customerSlug}/p/{propertySlug}/u/{unitSlug}/profile", Sections.Profile, activeSection),
            Link(T("Tenants", "Tenants"), $"/m/{companySlug}/c/{customerSlug}/p/{propertySlug}/u/{unitSlug}/tenants", Sections.Tenants, activeSection),
            Link(UiText.Tickets, $"/m/{companySlug}/c/{customerSlug}/p/{propertySlug}/u/{unitSlug}/tickets", Sections.Tickets, activeSection)
        };
    }

    private static IReadOnlyList<NavigationItemViewModel> BuildResidentNavigation(
        WorkspaceIdentityViewModel workspace,
        string activeSection)
    {
        var companySlug = workspace.ManagementCompanySlug ?? string.Empty;
        var residentIdCode = workspace.ResidentIdCode ?? string.Empty;

        return new List<NavigationItemViewModel>
        {
            Link(UiText.Dashboard, $"/m/{companySlug}/r/{residentIdCode}", Sections.Dashboard, activeSection),
            Link(UiText.Profile, $"/m/{companySlug}/r/{residentIdCode}/profile", Sections.Profile, activeSection),
            Link(T("Units", "Units"), $"/m/{companySlug}/r/{residentIdCode}/units", Sections.Units, activeSection),
            Link(UiText.Tickets, $"/m/{companySlug}/r/{residentIdCode}/tickets", Sections.Tickets, activeSection),
            Link(T("Representations", "Representations"), $"/m/{companySlug}/r/{residentIdCode}/representations", Sections.Representations, activeSection),
            Link(T("Contacts", "Contacts"), $"/m/{companySlug}/r/{residentIdCode}/contacts", Sections.Contacts, activeSection)
        };
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
