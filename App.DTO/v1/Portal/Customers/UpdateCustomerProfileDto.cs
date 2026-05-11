namespace App.DTO.v1.Portal.Customers;

public class UpdateCustomerProfileDto
{
    public string Name { get; set; } = string.Empty;
    public string RegistryCode { get; set; } = string.Empty;
    public string? BillingEmail { get; set; }
    public string? BillingAddress { get; set; }
    public string? Phone { get; set; }
}
