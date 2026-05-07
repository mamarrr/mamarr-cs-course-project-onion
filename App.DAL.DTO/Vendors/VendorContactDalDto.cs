using Base.Domain;

namespace App.DAL.DTO.Vendors;

public class VendorContactDalDto : BaseEntity
{
    public Guid VendorId { get; init; }
    public Guid ContactId { get; init; }
    public DateOnly ValidFrom { get; init; }
    public DateOnly? ValidTo { get; init; }
    public bool Confirmed { get; init; }
    public bool IsPrimary { get; init; }
    public string? FullName { get; init; }
    public string? RoleTitle { get; init; }
}

