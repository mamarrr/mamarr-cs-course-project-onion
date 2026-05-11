namespace App.DTO.v1.Portal.Dashboards;

public class PropertyDashboardContextDto : CustomerDashboardContextDto
{
    public Guid PropertyId { get; set; }
    public string PropertySlug { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string PropertyTypeLabel { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
}
