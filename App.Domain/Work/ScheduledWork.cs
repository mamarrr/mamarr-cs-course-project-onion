using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace App.Domain;

public class ScheduledWork : BaseEntity
{
    public DateTime ScheduledStart { get; set; }
    public DateTime? ScheduledEnd { get; set; }
    public DateTime? RealStart { get; set; }
    public DateTime? RealEnd { get; set; }
    [MinLength(1)]
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public Guid VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    public Guid WorkStatusId { get; set; }
    public WorkStatus? WorkStatus { get; set; }

    public ICollection<WorkLog>? WorkLogs { get; set; }
}
