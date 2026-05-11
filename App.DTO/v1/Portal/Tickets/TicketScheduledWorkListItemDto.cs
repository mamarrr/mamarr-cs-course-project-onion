namespace App.DTO.v1.Portal.Tickets;

public class TicketScheduledWorkListItemDto
{
    public Guid ScheduledWorkId { get; set; }
    public Guid VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public Guid WorkStatusId { get; set; }
    public string WorkStatusCode { get; set; } = string.Empty;
    public string WorkStatusLabel { get; set; } = string.Empty;
    public DateTime ScheduledStart { get; set; }
    public DateTime? ScheduledEnd { get; set; }
    public DateTime? RealStart { get; set; }
    public DateTime? RealEnd { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public int WorkLogCount { get; set; }
}
