using App.BLL.DTO.Units.Models;
using App.BLL.DTO.Units.Queries;
using FluentResults;

namespace App.BLL.Contracts.Units;

public interface IUnitAccessService
{
    Task<Result<UnitWorkspaceModel>> ResolveUnitWorkspaceAsync(
        GetUnitDashboardQuery query,
        CancellationToken cancellationToken = default);
}
