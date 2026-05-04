using App.BLL.Contracts.Units.Commands;
using App.BLL.Contracts.Units.Models;
using App.BLL.Contracts.Units.Queries;
using FluentResults;

namespace App.BLL.Contracts.Units;

public interface IUnitWorkspaceService
{
    Task<Result<UnitDashboardModel>> GetDashboardAsync(
        GetUnitDashboardQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<PropertyUnitsModel>> GetPropertyUnitsAsync(
        GetPropertyUnitsQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<UnitProfileModel>> CreateAsync(
        CreateUnitCommand command,
        CancellationToken cancellationToken = default);
}
