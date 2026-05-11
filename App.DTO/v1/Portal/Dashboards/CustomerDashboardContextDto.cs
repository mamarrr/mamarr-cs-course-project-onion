namespace App.DTO.v1.Portal.Dashboards;

public class CustomerDashboardContextDto : ManagementDashboardContextDto
{
    public Guid CustomerId { get; set; }
    public string CustomerSlug { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string RegistryCode { get; set; } = string.Empty;
    public string? BillingEmail { get; set; }
    public string? Phone { get; set; }
}
