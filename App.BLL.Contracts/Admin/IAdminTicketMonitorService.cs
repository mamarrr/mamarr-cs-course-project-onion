using App.BLL.DTO.Admin.Tickets;

namespace App.BLL.Contracts.Admin;

public interface IAdminTicketMonitorService
{
    Task<AdminTicketListDto> SearchTicketsAsync(AdminTicketSearchDto search, CancellationToken cancellationToken = default);
    Task<AdminTicketDetailsDto?> GetTicketDetailsAsync(Guid id, CancellationToken cancellationToken = default);
}
