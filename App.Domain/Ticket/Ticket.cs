using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Base.Contracts;
using Base.Domain;

namespace App.Domain;

public class Ticket : BaseEntity, IManagementCompanyId
{
    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string TicketNr { get; set; } = default!;

    [Required]
    [Display(ResourceType = typeof(App.Resources.Domain.Ticket), Name = nameof(App.Resources.Domain.Ticket.Title))]
    [Column(TypeName = "jsonb")]
    public LangStr Title { get; set; } = default!;

    [Required]
    [Display(ResourceType = typeof(App.Resources.Domain.Ticket), Name = nameof(App.Resources.Domain.Ticket.Description))]
    [Column(TypeName = "jsonb")]
    public LangStr Description { get; set; } = default!;
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
