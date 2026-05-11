namespace App.DTO.v1.Portal.Dashboards;

public class DashboardWorkPreviewDto
{
    public Guid ScheduledWorkId { get; set; }
    public Guid TicketId { get; set; }
    public string TicketNr { get; set; } = string.Empty;
    public string TicketTitle { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public string WorkStatusCode { get; set; } = string.Empty;
    public string WorkStatusLabel { get; set; } = string.Empty;
    public DateTime ScheduledStart { get; set; }
    public DateTime? ScheduledEnd { get; set; }
    public DateTime? RealStart { get; set; }
    public DateTime? RealEnd { get; set; }
    public string Path { get; set; } = string.Empty;
    public string EditPath { get; set; } = string.Empty;
    public string TicketPath { get; set; } = string.Empty;
}
