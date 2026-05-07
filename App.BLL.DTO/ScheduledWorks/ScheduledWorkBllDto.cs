using Base.Domain;

namespace App.BLL.DTO.ScheduledWorks;

public class ScheduledWorkBllDto : BaseEntity
{
    public Guid VendorId { get; set; }
    public Guid TicketId { get; set; }
    public Guid WorkStatusId { get; set; }
    public DateTime ScheduledStart { get; set; }
    public DateTime? ScheduledEnd { get; set; }
    public DateTime? RealStart { get; set; }
    public DateTime? RealEnd { get; set; }
    public string? Notes { get; set; }
}
