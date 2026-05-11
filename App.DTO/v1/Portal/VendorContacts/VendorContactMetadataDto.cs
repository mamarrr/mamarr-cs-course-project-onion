namespace App.DTO.v1.Portal.VendorContacts;

public abstract class VendorContactMetadataDto
{
    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? ValidTo { get; set; }
    public bool Confirmed { get; set; } = true;
    public bool IsPrimary { get; set; }
    public string? FullName { get; set; }
    public string? RoleTitle { get; set; }
}
