using App.BLL.Contracts.Onboarding.Commands;
using App.BLL.Contracts.Onboarding.Models;
using App.BLL.Contracts.Onboarding.Queries;
using FluentResults;

namespace App.BLL.Contracts.Onboarding.Services;

public interface IContextSelectionService
{
    Task<Result<WorkspaceCatalogModel>> GetWorkspaceCatalogAsync(
        GetWorkspaceCatalogQuery query,
        CancellationToken cancellationToken = default);

    Task<Result> SelectWorkspaceAsync(
        SelectWorkspaceCommand command,
        CancellationToken cancellationToken = default);
}
