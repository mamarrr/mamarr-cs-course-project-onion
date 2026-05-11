namespace App.DTO.v1.Portal.Vendors;

public class VendorCategoryAssignmentDto
{
    public Guid AssignmentId { get; set; }
    public Guid VendorId { get; set; }
    public Guid TicketCategoryId { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryLabel { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Path { get; set; } = string.Empty;
}
