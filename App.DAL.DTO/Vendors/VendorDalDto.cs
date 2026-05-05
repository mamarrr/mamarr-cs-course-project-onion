using Base.Contracts;

namespace App.DAL.DTO.Vendors;

public class VendorDalDto : IBaseEntity
{
    public Guid Id { get; set; }
    public Guid ManagementCompanyId { get; init; }
    public string Name { get; init; } = default!;
    public string RegistryCode { get; init; } = default!;
    public string Notes { get; init; } = default!;
    
    public DateTime CreatedAt { get; init; }
}
