using App.BLL.DTO.Onboarding.Models;
using App.BLL.DTO.Onboarding.Queries;
using FluentResults;

namespace App.BLL.Contracts.Onboarding;

public interface IWorkspaceRedirectService
{
    Task<Result<WorkspaceRedirectModel?>> ResolveContextRedirectAsync(
        ResolveWorkspaceRedirectQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<WorkspaceSelectionAuthorizationModel>> AuthorizeContextSelectionAsync(
        AuthorizeContextSelectionQuery query,
        CancellationToken cancellationToken = default);
}
