using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Base.Domain;

namespace App.Domain;

public class VendorTicketCategory : BaseEntity
{
    [Display(ResourceType = typeof(App.Resources.Domain.VendorTicketCategory), Name = nameof(App.Resources.Domain.VendorTicketCategory.Notes))]
    [Column(TypeName = "jsonb")]
    public LangStr? Notes { get; set; }
    public bool IsActive { get; set; }

    public Guid VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    public Guid TicketCategoryId { get; set; }
    public TicketCategory? TicketCategory { get; set; }
}
