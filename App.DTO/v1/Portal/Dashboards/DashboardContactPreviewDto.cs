namespace App.DTO.v1.Portal.Dashboards;

public class DashboardContactPreviewDto
{
    public Guid ContactId { get; set; }
    public string ContactTypeCode { get; set; } = string.Empty;
    public string ContactTypeLabel { get; set; } = string.Empty;
    public string ContactValue { get; set; } = string.Empty;
}
