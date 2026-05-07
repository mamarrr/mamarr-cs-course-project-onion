namespace App.BLL.DTO.Tickets.Models;

public class ManagementTicketFormModel
{
    public string CompanySlug { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public Guid? TicketId { get; init; }
    public string TicketNr { get; init; } = default!;
    public string Title { get; init; } = default!;
    public string Description { get; init; } = default!;
    public Guid TicketCategoryId { get; init; }
    public Guid TicketStatusId { get; init; }
    public Guid TicketPriorityId { get; init; }
    public Guid? CustomerId { get; init; }
    public Guid? PropertyId { get; init; }
    public Guid? UnitId { get; init; }
    public Guid? ResidentId { get; init; }
    public Guid? VendorId { get; init; }
    public DateTime? DueAt { get; init; }
    public TicketSelectorOptionsModel Options { get; init; } = new();
}
