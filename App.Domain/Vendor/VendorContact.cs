using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace App.Domain;

public class VendorContact : BaseEntity
{
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool Confirmed { get; set; }
    public bool IsPrimary { get; set; }
    [StringLength(200, MinimumLength = 1)]
    public string? FullName { get; set; }

    [StringLength(200, MinimumLength = 1)]
    public string? RoleTitle { get; set; }

    public Guid ContactId { get; set; }
    public Contact? Contact { get; set; }

    public Guid VendorId { get; set; }
    public Vendor? Vendor { get; set; }
}
