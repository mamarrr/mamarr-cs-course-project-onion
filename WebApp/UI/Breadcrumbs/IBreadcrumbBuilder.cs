using WebApp.UI.Workspace;

namespace WebApp.UI.Breadcrumbs;

public interface IBreadcrumbBuilder
{
    IReadOnlyList<BreadcrumbLinkViewModel> Build(WorkspaceIdentityViewModel workspace);
}
