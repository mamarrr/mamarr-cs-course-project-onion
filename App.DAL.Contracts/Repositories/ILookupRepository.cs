using App.DAL.DTO.Leases;
using App.DAL.DTO.Lookups;

namespace App.DAL.Contracts.Repositories;

public interface ILookupRepository
{
    Task<LookupDalDto?> FindManagementCompanyJoinRequestStatusByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LookupDalDto>> AllManagementCompanyJoinRequestStatusesAsync(
        CancellationToken cancellationToken = default);

    Task<LookupDalDto?> FindManagementCompanyRoleByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<LookupDalDto?> FindCustomerRepresentativeRoleByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<LookupDalDto?> FindLeaseRoleByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<bool> LeaseRoleExistsAsync(
        Guid leaseRoleId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaseRoleOptionDalDto>> ListLeaseRolesAsync(
        CancellationToken cancellationToken = default);

    Task<LookupDalDto?> FindPropertyTypeByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<LookupDalDto?> FindContactTypeByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);
}
