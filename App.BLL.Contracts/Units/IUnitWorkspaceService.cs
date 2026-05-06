using App.BLL.DTO.Units.Commands;
using App.BLL.DTO.Units.Models;
using App.BLL.DTO.Units.Queries;
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
