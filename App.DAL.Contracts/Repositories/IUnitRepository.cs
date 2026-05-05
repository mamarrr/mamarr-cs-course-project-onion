using App.DAL.DTO.Leases;
using App.DAL.DTO.Tickets;
using App.DAL.DTO.Units;
using Base.DAL.Contracts;

namespace App.DAL.Contracts.Repositories;

public interface IUnitRepository : IBaseRepository<UnitDalDto>
{
    Task<UnitDashboardDalDto?> FirstDashboardAsync(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        CancellationToken cancellationToken = default);

    Task<UnitProfileDalDto?> FirstProfileAsync(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        CancellationToken cancellationToken = default);

    Task<UnitProfileDalDto?> FindProfileAsync(
        Guid unitId,
        Guid propertyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UnitListItemDalDto>> AllByPropertyAsync(
        Guid propertyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> AllSlugsByPropertyWithPrefixAsync(
        Guid propertyId,
        string slugPrefix,
        CancellationToken cancellationToken = default);

    Task<bool> UnitSlugExistsForPropertyAsync(
        Guid propertyId,
        string slug,
        Guid? exceptUnitId = null,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsInCompanyAsync(
        Guid unitId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsInPropertyAsync(
        Guid unitId,
        Guid propertyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketOptionDalDto>> OptionsForTicketAsync(
        Guid managementCompanyId,
        Guid? propertyId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaseUnitOptionDalDto>> ListForLeaseAssignmentAsync(
        Guid propertyId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid unitId,
        Guid propertyId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);
}
