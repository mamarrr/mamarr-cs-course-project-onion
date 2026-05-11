namespace App.DTO.v1.Portal.Customers;

public class CustomerListItemDto
{
    public Guid CustomerId { get; set; }
    public Guid ManagementCompanyId { get; set; }
    public string CompanySlug { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string CustomerSlug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RegistryCode { get; set; } = string.Empty;
    public string? BillingEmail { get; set; }
    public string? BillingAddress { get; set; }
    public string? Phone { get; set; }
}
