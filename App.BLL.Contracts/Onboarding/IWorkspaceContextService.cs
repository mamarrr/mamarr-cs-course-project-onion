using App.BLL.Contracts.Onboarding.Models;
using FluentResults;

namespace App.BLL.Contracts.Onboarding;

public interface IWorkspaceContextService
{
    Task<Result<WorkspaceContextCatalogModel>> GetContextsAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default);
}
