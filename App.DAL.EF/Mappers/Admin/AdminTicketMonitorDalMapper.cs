using App.DAL.DTO.Admin.Tickets;
using App.Domain;

namespace App.DAL.EF.Mappers.Admin;

public class AdminTicketMonitorDalMapper
{
    public AdminTicketListItemDalDto MapListItem(Ticket ticket)
    {
        return new AdminTicketListItemDalDto
        {
            Id = ticket.Id,
            TicketNumber = ticket.TicketNr,
            Title = ticket.Title.ToString(),
            CompanyName = ticket.ManagementCompany?.Name ?? string.Empty,
            CustomerName = ticket.Customer?.Name,
            StatusCode = ticket.TicketStatus?.Code ?? string.Empty,
            StatusLabel = ticket.TicketStatus?.Label.ToString() ?? string.Empty,
            PriorityLabel = ticket.TicketPriority?.Label.ToString() ?? string.Empty,
            CategoryLabel = ticket.TicketCategory?.Label.ToString() ?? string.Empty,
            VendorName = ticket.Vendor?.Name,
            CreatedAt = ticket.CreatedAt,
            DueAt = ticket.DueAt,
            ClosedAt = ticket.ClosedAt,
            IsOverdue = ticket.DueAt.HasValue && ticket.DueAt.Value < DateTime.UtcNow && ticket.ClosedAt == null
        };
    }
}
