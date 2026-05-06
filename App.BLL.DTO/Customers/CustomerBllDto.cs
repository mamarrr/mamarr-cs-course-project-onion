using Base.Domain;

namespace App.BLL.DTO.Customers;

public class CustomerBllDto : BaseEntity
{
    public Guid ManagementCompanyId { get; set; }
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string RegistryCode { get; set; } = default!;
    public string? BillingEmail { get; set; }
    public string? BillingAddress { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }
}

