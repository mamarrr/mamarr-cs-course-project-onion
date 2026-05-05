using Base.Contracts;

namespace App.DAL.DTO.Customers;

public class CustomerDalDto : IBaseEntity
{
    public Guid Id { get; set; }
    public Guid ManagementCompanyId { get; set; }
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string RegistryCode { get; set; } = default!;
    public string? BillingEmail { get; set; }
    public string? BillingAddress { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; }
}
