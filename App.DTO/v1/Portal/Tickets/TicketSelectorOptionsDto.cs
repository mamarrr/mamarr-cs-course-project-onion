using App.DTO.v1.Shared;

namespace App.DTO.v1.Portal.Tickets;

public class TicketSelectorOptionsDto
{
    public IReadOnlyList<LookupOptionDto> Statuses { get; set; } = [];
    public IReadOnlyList<LookupOptionDto> Priorities { get; set; } = [];
    public IReadOnlyList<LookupOptionDto> Categories { get; set; } = [];
    public IReadOnlyList<LookupOptionDto> Customers { get; set; } = [];
    public IReadOnlyList<LookupOptionDto> Properties { get; set; } = [];
    public IReadOnlyList<LookupOptionDto> Units { get; set; } = [];
    public IReadOnlyList<LookupOptionDto> Residents { get; set; } = [];
    public IReadOnlyList<LookupOptionDto> Vendors { get; set; } = [];
}
