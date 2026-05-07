using App.DAL.DTO.Admin.Tickets;

namespace App.DAL.Contracts.Repositories.Admin;

public interface IAdminTicketMonitorRepository
{
    Task<IReadOnlyList<AdminTicketListItemDalDto>> SearchTicketsAsync(AdminTicketSearchDalDto search, CancellationToken cancellationToken = default);
    Task<AdminTicketDetailsDalDto?> GetTicketDetailsAsync(Guid id, CancellationToken cancellationToken = default);
}
