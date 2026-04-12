using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace App.Domain;

public class VendorTicketCategory : BaseEntity
{
    [MinLength(1)]
    public string? Notes { get; set; }
    public bool IsActive { get; set; }

    public Guid VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    public Guid TicketCategoryId { get; set; }
    public TicketCategory? TicketCategory { get; set; }
}
