using App.BLL.Contracts.Leases.Commands;
using App.BLL.Contracts.Leases.Models;
using App.BLL.Contracts.Leases.Queries;
using FluentResults;

namespace App.BLL.Contracts.Leases.Services;

public interface ILeaseAssignmentService
{
    Task<Result<ResidentLeaseListModel>> ListForResidentAsync(
        GetResidentLeasesQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<UnitLeaseListModel>> ListForUnitAsync(
        GetUnitLeasesQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<LeaseModel>> GetForResidentAsync(
        GetResidentLeaseQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<LeaseModel>> GetForUnitAsync(
        GetUnitLeaseQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<LeaseCommandModel>> CreateFromResidentAsync(
        CreateLeaseFromResidentCommand command,
        CancellationToken cancellationToken = default);

    Task<Result<LeaseCommandModel>> CreateFromUnitAsync(
        CreateLeaseFromUnitCommand command,
        CancellationToken cancellationToken = default);

    Task<Result<LeaseCommandModel>> UpdateFromResidentAsync(
        UpdateLeaseFromResidentCommand command,
        CancellationToken cancellationToken = default);

    Task<Result<LeaseCommandModel>> UpdateFromUnitAsync(
        UpdateLeaseFromUnitCommand command,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteFromResidentAsync(
        DeleteLeaseFromResidentCommand command,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteFromUnitAsync(
        DeleteLeaseFromUnitCommand command,
        CancellationToken cancellationToken = default);
}
