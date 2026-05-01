using WebApp.UI.Workspace;

namespace WebApp.UI.Breadcrumbs;

public class BreadcrumbLinkViewModel
{
    public string Label { get; init; } = string.Empty;

    public string? Url { get; init; }

    public bool IsCurrent { get; init; }

    public WorkspaceLevel Level { get; init; }
}
