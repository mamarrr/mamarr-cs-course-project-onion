namespace App.BLL.DTO.Tickets.Models;

public class TicketSelectorOptionsModel
{
    public IReadOnlyList<TicketOptionModel> Statuses { get; init; } = Array.Empty<TicketOptionModel>();
    public IReadOnlyList<TicketOptionModel> Priorities { get; init; } = Array.Empty<TicketOptionModel>();
    public IReadOnlyList<TicketOptionModel> Categories { get; init; } = Array.Empty<TicketOptionModel>();
    public IReadOnlyList<TicketOptionModel> Customers { get; init; } = Array.Empty<TicketOptionModel>();
    public IReadOnlyList<TicketOptionModel> Properties { get; init; } = Array.Empty<TicketOptionModel>();
    public IReadOnlyList<TicketOptionModel> Units { get; init; } = Array.Empty<TicketOptionModel>();
    public IReadOnlyList<TicketOptionModel> Residents { get; init; } = Array.Empty<TicketOptionModel>();
    public IReadOnlyList<TicketOptionModel> Vendors { get; init; } = Array.Empty<TicketOptionModel>();
}
