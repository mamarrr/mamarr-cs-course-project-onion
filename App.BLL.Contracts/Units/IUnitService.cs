using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Units;
using App.BLL.DTO.Units.Models;
using Base.BLL.Contracts;
using FluentResults;

namespace App.BLL.Contracts.Units;

public interface IUnitService : IBaseService<UnitBllDto>
{
    Task<Result<UnitWorkspaceModel>> ResolveWorkspaceAsync(
        UnitRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<PropertyUnitsModel>> ListForPropertyAsync(
        PropertyRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<UnitDashboardModel>> GetDashboardAsync(
        UnitRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<UnitProfileModel>> GetProfileAsync(
        UnitRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<UnitBllDto>> CreateAsync(
        PropertyRoute route,
        UnitBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<UnitProfileModel>> CreateAndGetProfileAsync(
        PropertyRoute route,
        UnitBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<UnitBllDto>> UpdateAsync(
        UnitRoute route,
        UnitBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<UnitProfileModel>> UpdateAndGetProfileAsync(
        UnitRoute route,
        UnitBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        UnitRoute route,
        string confirmationUnitNr,
        CancellationToken cancellationToken = default);
}
