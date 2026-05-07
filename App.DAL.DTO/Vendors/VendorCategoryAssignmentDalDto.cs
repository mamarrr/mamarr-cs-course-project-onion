namespace App.DAL.DTO.Vendors;

public class VendorCategoryAssignmentDalDto : VendorTicketCategoryDalDto
{
    public string CategoryCode { get; init; } = default!;
    public string CategoryLabel { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
}

