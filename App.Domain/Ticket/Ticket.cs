using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace App.Domain;

public class Ticket : BaseEntity
{
    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string TicketNr { get; set; } = default!;

    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string Title { get; set; } = default!;

    [Required]
    [MinLength(1)]
    public string Description { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public Guid ManagementCompanyId { get; set; }
    public ManagementCompany? ManagementCompany { get; set; }

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid? ResidentId { get; set; }
    public Resident? Resident { get; set; }

    public Guid? PropertyId { get; set; }
    public Property? Property { get; set; }

    public Guid? UnitId { get; set; }
    public Unit? Unit { get; set; }

    public Guid? TicketCategoryId { get; set; }
    public TicketCategory? TicketCategory { get; set; }

    public Guid? VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    public Guid TicketStatusId { get; set; }
    public TicketStatus? TicketStatus { get; set; }

    public Guid TicketPriorityId { get; set; }
    public TicketPriority? TicketPriority { get; set; }

    public ICollection<ScheduledWork>? ScheduledWorks { get; set; }
}
