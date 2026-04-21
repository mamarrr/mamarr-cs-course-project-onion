namespace App.BLL.Onboarding.ContextSelection;

public interface IWorkspaceRedirectService
{
    Task<WorkspaceRedirectTarget?> ResolveContextRedirectAsync(
        Guid appUserId,
        WorkspaceRedirectCookieState cookieState,
        CancellationToken cancellationToken = default);

    Task<WorkspaceRedirectAuthorizationResult> AuthorizeContextSelectionAsync(
        Guid appUserId,
        string contextType,
        Guid? contextId,
        CancellationToken cancellationToken = default);
}
