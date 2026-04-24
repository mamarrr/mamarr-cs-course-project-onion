namespace WebApp.UI.Workspace;

public sealed class WorkspaceResolutionResult
{
    public WorkspaceIdentityViewModel Workspace { get; init; } = new();

    public IReadOnlyList<WorkspaceSwitchOptionViewModel> ManagementWorkspaceOptions { get; init; } = [];

    public IReadOnlyList<WorkspaceSwitchOptionViewModel> CustomerWorkspaceOptions { get; init; } = [];

    public bool CanManageCompanyUsers { get; init; }
}
