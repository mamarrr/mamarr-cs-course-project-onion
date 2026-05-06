using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Leases;
using App.BLL.DTO.Leases.Models;
using Base.BLL.Contracts;
using FluentResults;

namespace App.BLL.Contracts.Leases;

public interface ILeaseService : IBaseService<LeaseBllDto>
{
    Task<Result<ResidentLeaseListModel>> ListForResidentAsync(
        ResidentRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<UnitLeaseListModel>> ListForUnitAsync(
        UnitRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<LeaseModel>> GetForResidentAsync(
        ResidentLeaseRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<LeaseModel>> GetForUnitAsync(
        UnitLeaseRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<LeaseBllDto>> CreateForResidentAsync(
        ResidentRoute route,
        LeaseBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<LeaseBllDto>> CreateForUnitAsync(
        UnitRoute route,
        LeaseBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<LeaseModel>> CreateForResidentAndGetDetailsAsync(
        ResidentRoute route,
        LeaseBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<LeaseModel>> CreateForUnitAndGetDetailsAsync(
        UnitRoute route,
        LeaseBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<LeaseBllDto>> UpdateFromResidentAsync(
        ResidentLeaseRoute route,
        LeaseBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<LeaseBllDto>> UpdateFromUnitAsync(
        UnitLeaseRoute route,
        LeaseBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<LeaseModel>> UpdateFromResidentAndGetDetailsAsync(
        ResidentLeaseRoute route,
        LeaseBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<LeaseModel>> UpdateFromUnitAndGetDetailsAsync(
        UnitLeaseRoute route,
        LeaseBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteFromResidentAsync(
        ResidentLeaseRoute route,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteFromUnitAsync(
        UnitLeaseRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<LeasePropertySearchResultModel>> SearchPropertiesAsync(
        ResidentRoute route,
        string? searchTerm,
        CancellationToken cancellationToken = default);

    Task<Result<LeaseUnitOptionsModel>> ListUnitsForPropertyAsync(
        ResidentRoute route,
        Guid propertyId,
        CancellationToken cancellationToken = default);

    Task<Result<LeaseResidentSearchResultModel>> SearchResidentsAsync(
        UnitRoute route,
        string? searchTerm,
        CancellationToken cancellationToken = default);

    Task<Result<LeaseRoleOptionsModel>> ListLeaseRolesAsync(
        CancellationToken cancellationToken = default);
}
