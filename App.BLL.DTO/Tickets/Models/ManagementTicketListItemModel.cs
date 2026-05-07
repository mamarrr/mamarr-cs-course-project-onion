namespace App.BLL.DTO.Tickets.Models;

public class ManagementTicketListItemModel
{
    public Guid TicketId { get; init; }
    public string TicketNr { get; init; } = default!;
    public string Title { get; init; } = default!;
    public string StatusCode { get; init; } = default!;
    public string StatusLabel { get; init; } = default!;
    public string PriorityLabel { get; init; } = default!;
    public string CategoryLabel { get; init; } = default!;
    public string? CustomerName { get; init; }
    public string? CustomerSlug { get; init; }
    public string? PropertyName { get; init; }
    public string? PropertySlug { get; init; }
    public string? UnitNr { get; init; }
    public string? UnitSlug { get; init; }
    public string? ResidentName { get; init; }
    public string? ResidentIdCode { get; init; }
    public string? VendorName { get; init; }
    public DateTime? DueAt { get; init; }
    public DateTime CreatedAt { get; init; }
}
