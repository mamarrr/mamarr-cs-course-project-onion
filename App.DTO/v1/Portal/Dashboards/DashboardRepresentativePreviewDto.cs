namespace App.DTO.v1.Portal.Dashboards;

public class DashboardRepresentativePreviewDto
{
    public Guid RepresentativeId { get; set; }
    public string ResidentIdCode { get; set; } = string.Empty;
    public string ResidentName { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public string RoleLabel { get; set; } = string.Empty;
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public string? CustomerSlug { get; set; }
    public string? CustomerName { get; set; }
}
