namespace App.DTO.v1.Portal.Dashboards;

public class ResidentDashboardContextDto : ManagementDashboardContextDto
{
    public Guid ResidentId { get; set; }
    public string ResidentIdCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PreferredLanguage { get; set; }
}
