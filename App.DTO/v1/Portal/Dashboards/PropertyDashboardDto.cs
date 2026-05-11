namespace App.DTO.v1.Portal.Dashboards;

public class PropertyDashboardDto
{
    public PropertyDashboardContextDto Context { get; set; } = new();
    public IReadOnlyList<DashboardMetricDto> UnitMetrics { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemDto> UnitsByFloor { get; set; } = [];
    public IReadOnlyList<DashboardMetricDto> TicketMetrics { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemDto> TicketsByStatus { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemDto> TicketsByPriority { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemDto> TicketsByCategory { get; set; } = [];
    public IReadOnlyList<DashboardTicketPreviewDto> RecentTickets { get; set; } = [];
    public IReadOnlyList<DashboardMetricDto> ResidentLeaseMetrics { get; set; } = [];
    public IReadOnlyList<DashboardLeasePreviewDto> CurrentLeases { get; set; } = [];
    public IReadOnlyList<DashboardWorkPreviewDto> UpcomingWork { get; set; } = [];
    public IReadOnlyList<DashboardWorkPreviewDto> DelayedWork { get; set; } = [];
    public IReadOnlyList<DashboardWorkPreviewDto> RecentlyCompletedWork { get; set; } = [];
    public IReadOnlyList<DashboardUnitPreviewDto> UnitPreview { get; set; } = [];
}
