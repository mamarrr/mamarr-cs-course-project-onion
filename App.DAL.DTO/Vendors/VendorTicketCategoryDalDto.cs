using Base.Domain;

namespace App.DAL.DTO.Vendors;

public class VendorTicketCategoryDalDto : BaseEntity
{
    public Guid VendorId { get; init; }
    public Guid TicketCategoryId { get; init; }
    public string? Notes { get; init; }
}

