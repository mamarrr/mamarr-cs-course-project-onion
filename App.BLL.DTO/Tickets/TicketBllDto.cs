using Base.Domain;

namespace App.BLL.DTO.Tickets;

public class TicketBllDto : BaseEntity
{
    public Guid ManagementCompanyId { get; set; }
    public string TicketNr { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
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

