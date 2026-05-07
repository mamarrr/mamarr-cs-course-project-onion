namespace App.DAL.DTO.Vendors;

public class VendorContactAssignmentDalDto : VendorContactDalDto
{
    public Guid ContactTypeId { get; init; }
    public string ContactTypeCode { get; init; } = default!;
    public string ContactTypeLabel { get; init; } = default!;
    public string ContactValue { get; init; } = default!;
    public string? ContactNotes { get; init; }
    public DateTime CreatedAt { get; init; }
}

