namespace App.DTO.v1.Portal.Tickets;

public class TicketSelectorOptionsDto
{
    public IReadOnlyList<TicketOptionDto> Statuses { get; set; } = [];
    public IReadOnlyList<TicketOptionDto> Priorities { get; set; } = [];
    public IReadOnlyList<TicketOptionDto> Categories { get; set; } = [];
    public IReadOnlyList<TicketOptionDto> Customers { get; set; } = [];
    public IReadOnlyList<TicketOptionDto> Properties { get; set; } = [];
    public IReadOnlyList<TicketOptionDto> Units { get; set; } = [];
    public IReadOnlyList<TicketOptionDto> Residents { get; set; } = [];
    public IReadOnlyList<TicketOptionDto> Vendors { get; set; } = [];
}
