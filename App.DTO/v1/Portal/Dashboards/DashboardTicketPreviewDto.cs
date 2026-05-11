namespace App.DTO.v1.Portal.Dashboards;

public class DashboardTicketPreviewDto
{
    public Guid TicketId { get; set; }
    public string TicketNr { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public string PriorityCode { get; set; } = string.Empty;
    public string PriorityLabel { get; set; } = string.Empty;
    public DateTime? DueAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? CustomerSlug { get; set; }
    public string? CustomerName { get; set; }
    public string? PropertySlug { get; set; }
    public string? PropertyName { get; set; }
    public string? UnitSlug { get; set; }
    public string? UnitNr { get; set; }
    public string? ResidentIdCode { get; set; }
    public string? ResidentName { get; set; }
    public string Path { get; set; } = string.Empty;
    public string EditPath { get; set; } = string.Empty;
}
