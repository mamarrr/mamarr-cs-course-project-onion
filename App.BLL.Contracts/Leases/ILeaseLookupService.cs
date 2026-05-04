using App.BLL.Contracts.Leases.Models;
using App.BLL.Contracts.Leases.Queries;
using FluentResults;

namespace App.BLL.Contracts.Leases;

public interface ILeaseLookupService
{
    Task<Result<LeasePropertySearchResultModel>> SearchPropertiesAsync(
        SearchLeasePropertiesQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<LeaseUnitOptionsModel>> ListUnitsForPropertyAsync(
        GetLeaseUnitsForPropertyQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<LeaseResidentSearchResultModel>> SearchResidentsAsync(
        SearchLeaseResidentsQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<LeaseRoleOptionsModel>> ListLeaseRolesAsync(
        CancellationToken cancellationToken = default);
}
