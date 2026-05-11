namespace App.DTO.v1.Portal.Tickets;

public class TicketDto
{
    public Guid TicketId { get; set; }
    public Guid ManagementCompanyId { get; set; }
    public string TicketNr { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid TicketCategoryId { get; set; }
    public Guid TicketStatusId { get; set; }
    public Guid TicketPriorityId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? ResidentId { get; set; }
    public Guid? VendorId { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}
