using App.BLL.Contracts.Admin;
using App.BLL.DTO.Admin.Tickets;
using App.BLL.Mappers.Admin;
using App.DAL.Contracts;

namespace App.BLL.Services.Admin;

public class AdminTicketMonitorService : IAdminTicketMonitorService
{
    private readonly IAppUOW _uow;
    private readonly AdminTicketMonitorBllMapper _mapper = new();

    public AdminTicketMonitorService(IAppUOW uow)
    {
        _uow = uow;
    }

    public async Task<AdminTicketListDto> SearchTicketsAsync(AdminTicketSearchDto search, CancellationToken cancellationToken = default)
    {
        var tickets = await _uow.AdminTicketMonitor.SearchTicketsAsync(_mapper.Map(search), cancellationToken);
        return new AdminTicketListDto
        {
            Search = search,
            Tickets = tickets.Select(_mapper.Map).ToList()
        };
    }

    public async Task<AdminTicketDetailsDto?> GetTicketDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ticket = await _uow.AdminTicketMonitor.GetTicketDetailsAsync(id, cancellationToken);
        return ticket is null ? null : _mapper.Map(ticket);
    }
}
