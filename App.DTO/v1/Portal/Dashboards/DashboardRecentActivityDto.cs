namespace App.DTO.v1.Portal.Dashboards;

public class DashboardRecentActivityDto
{
    public string ItemType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? SupportingText { get; set; }
    public DateTime EventAt { get; set; }
    public string? CustomerSlug { get; set; }
    public string? PropertySlug { get; set; }
    public string? UnitSlug { get; set; }
    public string? ResidentIdCode { get; set; }
    public Guid? TicketId { get; set; }
    public string Path { get; set; } = string.Empty;
}
