namespace App.DTO.v1.Portal.Properties;

public class PropertyProfileDto
{
    public Guid PropertyId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ManagementCompanyId { get; set; }
    public string CompanySlug { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string CustomerSlug { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string PropertySlug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public Guid PropertyTypeId { get; set; }
    public string PropertyTypeCode { get; set; } = string.Empty;
    public string PropertyTypeLabel { get; set; } = string.Empty;
}
