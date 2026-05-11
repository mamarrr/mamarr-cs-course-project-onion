namespace App.DTO.v1.Portal.Dashboards;

public class CustomerDashboardDto
{
    public CustomerDashboardContextDto Context { get; set; } = new();
    public IReadOnlyList<DashboardMetricDto> PortfolioMetrics { get; set; } = [];
    public IReadOnlyList<DashboardMetricDto> TicketMetrics { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemDto> TicketsByProperty { get; set; } = [];
    public IReadOnlyList<DashboardTicketPreviewDto> RecentTickets { get; set; } = [];
    public IReadOnlyList<DashboardRecentActivityDto> RecentActivity { get; set; } = [];
}
