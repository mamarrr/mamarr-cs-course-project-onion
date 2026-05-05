using App.DAL.DTO.Tickets;
using Base.DAL.Contracts;

namespace App.DAL.Contracts.Repositories;

public interface ITicketRepository : IBaseRepository<TicketDalDto>
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

    Task<bool> UpdateStatusAsync(
        TicketStatusUpdateDalDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> HasDeleteDependenciesAsync(
        Guid ticketId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);
}
