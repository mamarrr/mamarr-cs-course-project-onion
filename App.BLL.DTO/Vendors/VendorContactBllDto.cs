using Base.Domain;

namespace App.BLL.DTO.Vendors;

public class VendorContactBllDto : BaseEntity
{
    public Guid VendorId { get; set; }
    public Guid ContactId { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool Confirmed { get; set; }
    public bool IsPrimary { get; set; }
    public string? FullName { get; set; }
    public string? RoleTitle { get; set; }
}

