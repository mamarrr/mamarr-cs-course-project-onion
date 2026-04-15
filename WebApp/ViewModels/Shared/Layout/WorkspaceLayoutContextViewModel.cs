using App.Resources.Views;

namespace WebApp.ViewModels.Shared.Layout;

public class WorkspaceLayoutContextViewModel
{
    public string CurrentController { get; init; } = string.Empty;
    public string CompanySlug { get; init; } = string.Empty;
    public string WorkspaceName { get; init; } = UiText.ManagementWorkspace;
    public bool HasResidentContext { get; init; }
    public string CurrentPathAndQuery { get; init; } = string.Empty;
    public string CurrentUiCultureName { get; init; } = string.Empty;

    public IReadOnlyCollection<WorkspaceLayoutContextOptionViewModel> ManagementContexts { get; init; } = [];
    public IReadOnlyCollection<WorkspaceLayoutContextOptionViewModel> CustomerContexts { get; init; } = [];
    public IReadOnlyCollection<WorkspaceLayoutCultureOptionViewModel> CultureOptions { get; init; } = [];
}

public class WorkspaceLayoutContextOptionViewModel
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public class WorkspaceLayoutCultureOptionViewModel
{
    public string Value { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public bool IsCurrent { get; init; }
}
