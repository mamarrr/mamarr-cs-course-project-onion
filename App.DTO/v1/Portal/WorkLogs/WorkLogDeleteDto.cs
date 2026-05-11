namespace App.DTO.v1.Portal.WorkLogs;

public class WorkLogDeleteDto
{
    public string CompanySlug { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public Guid TicketId { get; set; }
    public string TicketNr { get; set; } = string.Empty;
    public Guid ScheduledWorkId { get; set; }
    public Guid WorkLogId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Path { get; set; } = string.Empty;
}
