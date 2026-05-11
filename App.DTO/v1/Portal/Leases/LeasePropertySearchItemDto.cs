namespace App.DTO.v1.Portal.Leases;

public class LeasePropertySearchItemDto
{
    public Guid PropertyId { get; set; }
    public Guid CustomerId { get; set; }
    public string PropertySlug { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string CustomerSlug { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
}
