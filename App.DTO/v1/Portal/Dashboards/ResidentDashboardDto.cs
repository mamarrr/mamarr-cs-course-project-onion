namespace App.DTO.v1.Portal.Dashboards;

public class ResidentDashboardDto
{
    public ResidentDashboardContextDto Context { get; set; } = new();
    public IReadOnlyList<DashboardLeasePreviewDto> ActiveLeases { get; set; } = [];
    public IReadOnlyList<DashboardMetricDto> TicketMetrics { get; set; } = [];
    public IReadOnlyList<DashboardTicketPreviewDto> RecentTickets { get; set; } = [];
    public DashboardContactSummaryDto ContactSummary { get; set; } = new();
    public IReadOnlyList<DashboardRepresentativePreviewDto> Representations { get; set; } = [];
}
