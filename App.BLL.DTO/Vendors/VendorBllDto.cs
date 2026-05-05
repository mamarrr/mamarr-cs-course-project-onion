using Base.Domain;

namespace App.BLL.Contracts.Vendors;

public class VendorBllDto : BaseEntity
{
    public Guid ManagementCompanyId { get; set; }
    public string Name { get; set; } = default!;
    public string RegistryCode { get; set; } = default!;
    public string Notes { get; set; } = default!;
}

