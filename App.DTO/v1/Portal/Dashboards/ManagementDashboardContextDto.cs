namespace App.DTO.v1.Portal.Dashboards;

public class ManagementDashboardContextDto
{
    public Guid ManagementCompanyId { get; set; }
    public string CompanySlug { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string ApiPath { get; set; } = string.Empty;
}
