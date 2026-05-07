namespace App.BLL.DTO.Tickets.Models;

public class ManagementTicketsModel
{
    public string CompanySlug { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public IReadOnlyList<ManagementTicketListItemModel> Tickets { get; init; } = Array.Empty<ManagementTicketListItemModel>();
    public TicketFilterModel Filter { get; init; } = new();
    public TicketSelectorOptionsModel Options { get; init; } = new();
}
