using App.BLL.Contracts.Units.Models;
using App.BLL.Contracts.Units.Queries;
using FluentResults;

namespace App.BLL.Contracts.Units.Services;

public interface IUnitAccessService
{
    Task<Result<UnitWorkspaceModel>> ResolveUnitWorkspaceAsync(
        GetUnitDashboardQuery query,
        CancellationToken cancellationToken = default);
}
