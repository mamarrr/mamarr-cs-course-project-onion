namespace App.DTO.v1.Portal.ScheduledWork;

public class ScheduledWorkDto
{
    public Guid ScheduledWorkId { get; set; }
    public Guid VendorId { get; set; }
    public Guid WorkStatusId { get; set; }
    public DateTime ScheduledStart { get; set; }
    public DateTime? ScheduledEnd { get; set; }
    public DateTime? RealStart { get; set; }
    public DateTime? RealEnd { get; set; }
    public string? Notes { get; set; }
    public string Path { get; set; } = string.Empty;
}
