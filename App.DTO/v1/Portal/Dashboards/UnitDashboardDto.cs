namespace App.DTO.v1.Portal.Dashboards;

public class UnitDashboardDto
{
    public UnitDashboardContextDto Context { get; set; } = new();
    public DashboardLeasePreviewDto? CurrentLease { get; set; }
    public IReadOnlyList<DashboardMetricDto> TicketMetrics { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemDto> TicketsByStatus { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemDto> TicketsByPriority { get; set; } = [];
    public IReadOnlyList<DashboardTicketPreviewDto> RecentTickets { get; set; } = [];
    public IReadOnlyList<DashboardWorkPreviewDto> UpcomingWork { get; set; } = [];
    public IReadOnlyList<DashboardWorkPreviewDto> DelayedWork { get; set; } = [];
    public IReadOnlyList<DashboardWorkPreviewDto> RecentlyCompletedWork { get; set; } = [];
    public IReadOnlyList<DashboardTimelineItemDto> Timeline { get; set; } = [];
}
