using Base.DAL.Contracts;

namespace App.Contracts.DAL.Leases;

public interface ILeaseRepository : IBaseRepository<LeaseDalDto>
{
    Task<IReadOnlyList<ResidentLeaseDalDto>> AllByResidentAsync(
        Guid residentId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UnitLeaseDalDto>> AllByUnitAsync(
        Guid unitId,
        Guid propertyId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<LeaseDetailsDalDto?> FirstByIdForResidentAsync(
        Guid leaseId,
        Guid residentId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<LeaseDetailsDalDto?> FirstByIdForUnitAsync(
        Guid leaseId,
        Guid unitId,
        Guid propertyId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> LeaseRoleExistsAsync(
        Guid leaseRoleId,
        CancellationToken cancellationToken = default);

    Task<bool> UnitExistsInCompanyAsync(
        Guid unitId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> ResidentExistsInCompanyAsync(
        Guid residentId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> PropertyExistsInCompanyAsync(
        Guid propertyId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> HasOverlappingActiveLeaseAsync(
        Guid residentId,
        Guid unitId,
        DateOnly startDate,
        Guid? exceptLeaseId = null,
        CancellationToken cancellationToken = default);

    Task<LeaseDalDto> AddAsync(
        LeaseCreateDalDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateForResidentAsync(
        Guid residentId,
        Guid managementCompanyId,
        LeaseUpdateDalDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateForUnitAsync(
        Guid unitId,
        Guid propertyId,
        Guid managementCompanyId,
        LeaseUpdateDalDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteForResidentAsync(
        Guid leaseId,
        Guid residentId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteForUnitAsync(
        Guid leaseId,
        Guid unitId,
        Guid propertyId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeasePropertySearchItemDalDto>> SearchPropertiesAsync(
        Guid managementCompanyId,
        string? searchTerm,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaseUnitOptionDalDto>> ListUnitsForPropertyAsync(
        Guid propertyId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaseResidentSearchItemDalDto>> SearchResidentsAsync(
        Guid managementCompanyId,
        string? searchTerm,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaseRoleOptionDalDto>> ListLeaseRolesAsync(
        CancellationToken cancellationToken = default);
}
