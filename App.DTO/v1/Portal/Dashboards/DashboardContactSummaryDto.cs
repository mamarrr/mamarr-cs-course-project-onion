namespace App.DTO.v1.Portal.Dashboards;

public class DashboardContactSummaryDto
{
    public DashboardContactPreviewDto? PrimaryContact { get; set; }
    public IReadOnlyList<DashboardBreakdownItemDto> ContactMethodCounts { get; set; } = [];
}
