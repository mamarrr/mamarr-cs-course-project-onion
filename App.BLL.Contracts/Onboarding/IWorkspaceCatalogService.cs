using App.BLL.DTO.Onboarding.Models;
using App.BLL.DTO.Onboarding.Queries;
using FluentResults;

namespace App.BLL.Contracts.Onboarding;

public interface IWorkspaceCatalogService
{
    Task<Result<WorkspaceCatalogModel>> GetWorkspaceCatalogAsync(
        GetWorkspaceCatalogQuery query,
        CancellationToken cancellationToken = default);
}
