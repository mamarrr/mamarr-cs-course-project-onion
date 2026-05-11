namespace App.DTO.v1.Portal.Properties;

public class PropertyListItemDto
{
    public Guid PropertyId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ManagementCompanyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string PropertySlug { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public Guid PropertyTypeId { get; set; }
    public string PropertyTypeCode { get; set; } = string.Empty;
    public string PropertyTypeLabel { get; set; } = string.Empty;
}
