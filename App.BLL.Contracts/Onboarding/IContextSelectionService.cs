using App.BLL.DTO.Onboarding.Commands;
using App.BLL.DTO.Onboarding.Models;
using App.BLL.DTO.Onboarding.Queries;
using FluentResults;

namespace App.BLL.Contracts.Onboarding;

public interface IContextSelectionService
{
    Task<Result<WorkspaceCatalogModel>> GetWorkspaceCatalogAsync(
        GetWorkspaceCatalogQuery query,
        CancellationToken cancellationToken = default);

    Task<Result> SelectWorkspaceAsync(
        SelectWorkspaceCommand command,
        CancellationToken cancellationToken = default);
}
