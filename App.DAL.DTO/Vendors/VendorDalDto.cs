using Base.Domain;

namespace App.DAL.DTO.Vendors;

public class VendorDalDto : BaseEntity
{
    public Guid ManagementCompanyId { get; init; }
    public string Name { get; init; } = default!;
    public string RegistryCode { get; init; } = default!;
    public string Notes { get; init; } = default!;
}
