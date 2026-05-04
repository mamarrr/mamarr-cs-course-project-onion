using Base.Contracts;

namespace App.DAL.DTO.Customers;

public class CustomerDalDto : IBaseEntity
{
    public Guid Id { get; set; }
    public Guid ManagementCompanyId { get; set; }
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string RegistryCode { get; set; } = default!;
    public bool IsActive { get; set; }
}
