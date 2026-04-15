using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
    [Display(ResourceType = typeof(App.Resources.Domain.VendorContact), Name = nameof(App.Resources.Domain.VendorContact.RoleTitle))]
    [Column(TypeName = "jsonb")]
    public LangStr? RoleTitle { get; set; }

    public Guid ContactId { get; set; }
    public Contact? Contact { get; set; }

    public Guid VendorId { get; set; }
    public Vendor? Vendor { get; set; }
}
