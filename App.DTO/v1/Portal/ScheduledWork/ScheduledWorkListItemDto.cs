namespace App.DTO.v1.Portal.ScheduledWork;

public class ScheduledWorkListDto
{
    public string CompanySlug { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public Guid TicketId { get; set; }
    public string TicketNr { get; set; } = string.Empty;
    public string TicketTitle { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public IReadOnlyList<ScheduledWorkListItemDto> Items { get; set; } = [];
}

public class ScheduledWorkListItemDto
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
    public string Path { get; set; } = string.Empty;
    public string WorkLogsPath { get; set; } = string.Empty;
}
