using App.DAL.DTO.Tickets;

namespace App.DAL.Contracts.Repositories;

public interface ITicketRepository
{
    Task<IReadOnlyList<TicketListItemDalDto>> AllByCompanyAsync(
        Guid managementCompanyId,
        TicketListFilterDalDto filter,
        CancellationToken cancellationToken = default);

    Task<TicketDetailsDalDto?> FindDetailsAsync(
        Guid ticketId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<TicketEditDalDto?> FindForEditAsync(
        Guid ticketId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<string> GetNextTicketNrAsync(
        Guid managementCompanyId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task<bool> TicketNrExistsAsync(
        Guid managementCompanyId,
        string ticketNr,
        Guid? exceptTicketId = null,
        CancellationToken cancellationToken = default);

    Task<TicketOptionDalDto?> FindStatusByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<TicketOptionDalDto?> FindStatusByIdAsync(
        Guid statusId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketOptionDalDto>> AllStatusesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketOptionDalDto>> AllPrioritiesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketOptionDalDto>> AllCategoriesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketOptionDalDto>> CustomerOptionsAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketOptionDalDto>> PropertyOptionsAsync(
        Guid managementCompanyId,
        Guid? customerId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketOptionDalDto>> UnitOptionsAsync(
        Guid managementCompanyId,
        Guid? propertyId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketOptionDalDto>> ResidentOptionsAsync(
        Guid managementCompanyId,
        Guid? unitId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketOptionDalDto>> VendorOptionsAsync(
        Guid managementCompanyId,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default);

    Task<TicketReferenceValidationDalDto> ValidateReferencesAsync(
        Guid managementCompanyId,
        Guid categoryId,
        Guid priorityId,
        Guid statusId,
        Guid? customerId,
        Guid? propertyId,
        Guid? unitId,
        Guid? residentId,
        Guid? vendorId,
        CancellationToken cancellationToken = default);

    Task<Guid> AddAsync(
        TicketCreateDalDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        TicketUpdateDalDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateStatusAsync(
        TicketStatusUpdateDalDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid ticketId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> AllIdsForCustomerScopeAsync(
        Guid customerId,
        IReadOnlyCollection<Guid> propertyIds,
        IReadOnlyCollection<Guid> unitIds,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> AllIdsForPropertyScopeAsync(
        Guid propertyId,
        IReadOnlyCollection<Guid> unitIds,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> AllIdsForUnitScopeAsync(
        Guid unitId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> AllIdsForResidentScopeAsync(
        Guid residentId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task DeleteDependentsByTicketIdsAsync(
        IReadOnlyCollection<Guid> ticketIds,
        CancellationToken cancellationToken = default);

    Task DeleteByIdsAsync(
        IReadOnlyCollection<Guid> ticketIds,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);
}
