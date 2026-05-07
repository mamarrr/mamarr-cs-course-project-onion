using App.DAL.DTO.Leases;
using App.DAL.DTO.Residents;
using App.DAL.DTO.Tickets;
using Base.DAL.Contracts;

namespace App.DAL.Contracts.Repositories;

public interface IResidentRepository : IBaseRepository<ResidentDalDto>
{
    Task<ResidentProfileDalDto?> FirstProfileAsync(
        string companySlug,
        string residentIdCode,
        CancellationToken cancellationToken = default);

    Task<ResidentProfileDalDto?> FindProfileAsync(
        Guid residentId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResidentListItemDalDto>> AllByCompanyAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<ResidentUserContextDalDto?> FirstActiveUserResidentContextAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveUserResidentContextAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveUserResidentContextAsync(
        Guid appUserId,
        Guid residentId,
        CancellationToken cancellationToken = default);

    Task<bool> IdCodeExistsForCompanyAsync(
        Guid managementCompanyId,
        string idCode,
        Guid? exceptResidentId = null,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsInCompanyAsync(
        Guid residentId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> IsLinkedToUnitAsync(
        Guid residentId,
        Guid unitId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketOptionDalDto>> OptionsForTicketAsync(
        Guid managementCompanyId,
        Guid? unitId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaseResidentSearchItemDalDto>> SearchForLeaseAssignmentAsync(
        Guid managementCompanyId,
        string? searchTerm,
        CancellationToken cancellationToken = default);

    Task<bool> HasDeleteDependenciesAsync(
        Guid residentId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResidentContactAssignmentDalDto>> ContactsByResidentAsync(
        Guid residentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResidentLeaseSummaryDalDto>> LeaseSummariesByResidentAsync(
        Guid residentId,
        CancellationToken cancellationToken = default);
}
