using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Base.Contracts;
using Base.Domain;

namespace App.Domain;

public class ScheduledWork : BaseEntity, IHasCreatedAtMeta
{
    public DateTime ScheduledStart { get; set; }
    public DateTime? ScheduledEnd { get; set; }
    public DateTime? RealStart { get; set; }
    public DateTime? RealEnd { get; set; }
    [Display(ResourceType = typeof(App.Resources.Domain.ScheduledWork), Name = nameof(App.Resources.Domain.ScheduledWork.Notes))]
    [Column(TypeName = "jsonb")]
    public LangStr? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public Guid VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    public Guid WorkStatusId { get; set; }
    public WorkStatus? WorkStatus { get; set; }

    public ICollection<WorkLog>? WorkLogs { get; set; }
}
