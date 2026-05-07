using Base.Domain;

namespace App.BLL.DTO.Vendors;

public class VendorTicketCategoryBllDto : BaseEntity
{
    public Guid VendorId { get; set; }
    public Guid TicketCategoryId { get; set; }
    public string? Notes { get; set; }
}

