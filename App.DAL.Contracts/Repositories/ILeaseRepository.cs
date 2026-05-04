using App.DAL.DTO.Leases;
using Base.DAL.Contracts;

namespace App.DAL.Contracts.Repositories;

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

    Task DeleteByUnitIdsAsync(
        IReadOnlyCollection<Guid> unitIds,
        CancellationToken cancellationToken = default);

    Task DeleteByResidentIdAsync(
        Guid residentId,
        CancellationToken cancellationToken = default);
}
