namespace App.DTO.v1.Portal.Tickets;

public class ManagementTicketsDto
{
    public string CompanySlug { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public IReadOnlyList<TicketListItemDto> Tickets { get; set; } = [];
    public TicketFilterDto Filter { get; set; } = new();
    public TicketSelectorOptionsDto Options { get; set; } = new();
}
