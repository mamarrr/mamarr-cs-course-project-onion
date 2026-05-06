namespace WebApp.UI.Workspace;

public class WorkspaceResolutionResult
{
    public WorkspaceIdentityViewModel Workspace { get; init; } = new();

    public IReadOnlyList<WorkspaceSwitchOptionViewModel> ManagementWorkspaceOptions { get; init; } = [];

    public IReadOnlyList<WorkspaceSwitchOptionViewModel> CustomerWorkspaceOptions { get; init; } = [];

    public WorkspaceSwitchOptionViewModel? ResidentWorkspaceOption { get; init; }

    public bool CanManageCompanyUsers { get; init; }
}
