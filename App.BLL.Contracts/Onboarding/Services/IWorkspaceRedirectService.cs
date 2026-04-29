using App.BLL.Contracts.Onboarding.Models;
using App.BLL.Contracts.Onboarding.Queries;
using FluentResults;

namespace App.BLL.Contracts.Onboarding.Services;

public interface IWorkspaceRedirectService
{
    Task<Result<WorkspaceRedirectModel?>> ResolveContextRedirectAsync(
        ResolveWorkspaceRedirectQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<WorkspaceSelectionAuthorizationModel>> AuthorizeContextSelectionAsync(
        AuthorizeContextSelectionQuery query,
        CancellationToken cancellationToken = default);
}
