using WebApp.UI.Chrome;

namespace WebApp.UI.Workspace;

public interface IWorkspaceResolver
{
    Task<WorkspaceResolutionResult> ResolveAsync(
        AppChromeRequest request,
        CancellationToken cancellationToken = default);
}
