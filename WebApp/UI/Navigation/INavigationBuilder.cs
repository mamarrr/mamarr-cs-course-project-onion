using WebApp.UI.Workspace;

namespace WebApp.UI.Navigation;

public interface INavigationBuilder
{
    IReadOnlyList<NavigationItemViewModel> Build(
        WorkspaceIdentityViewModel workspace,
        string activeSection,
        bool canManageCompanyUsers);
}
