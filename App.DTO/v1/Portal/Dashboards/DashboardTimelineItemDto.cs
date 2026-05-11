namespace App.DTO.v1.Portal.Dashboards;

public class DashboardTimelineItemDto
{
    public string ItemType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? SupportingText { get; set; }
    public DateTime EventAt { get; set; }
    public Guid? TicketId { get; set; }
    public Guid? ScheduledWorkId { get; set; }
    public Guid? LeaseId { get; set; }
    public string Path { get; set; } = string.Empty;
}
