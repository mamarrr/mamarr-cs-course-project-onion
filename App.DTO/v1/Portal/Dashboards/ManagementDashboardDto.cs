namespace App.DTO.v1.Portal.Dashboards;

public class ManagementDashboardDto
{
    public ManagementDashboardContextDto Context { get; set; } = new();
    public IReadOnlyList<DashboardMetricDto> SummaryMetrics { get; set; } = [];
    public IReadOnlyList<DashboardMetricDto> TicketMetrics { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemDto> TicketsByStatus { get; set; } = [];
    public IReadOnlyList<DashboardTicketPreviewDto> RecentTickets { get; set; } = [];
    public IReadOnlyList<DashboardMetricDto> WorkMetrics { get; set; } = [];
    public IReadOnlyList<DashboardWorkPreviewDto> RecentCompletedWork { get; set; } = [];
    public IReadOnlyList<DashboardRecentActivityDto> RecentActivity { get; set; } = [];
}
