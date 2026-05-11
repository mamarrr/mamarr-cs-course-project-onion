namespace App.DTO.v1.Portal.VendorContacts;

public class AttachExistingVendorContactDto : VendorContactMetadataDto
{
    public Guid ContactId { get; set; }
}

public class CreateAndAttachVendorContactDto : VendorContactMetadataDto
{
    public Guid ContactTypeId { get; set; }
    public string ContactValue { get; set; } = string.Empty;
    public string? ContactNotes { get; set; }
}

public class UpdateVendorContactDto : VendorContactMetadataDto
{
    public Guid ContactId { get; set; }
}

public abstract class VendorContactMetadataDto
{
    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? ValidTo { get; set; }
    public bool Confirmed { get; set; } = true;
    public bool IsPrimary { get; set; }
    public string? FullName { get; set; }
    public string? RoleTitle { get; set; }
}
