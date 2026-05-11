namespace App.DTO.v1.Portal.VendorContacts;

public class CreateAndAttachVendorContactDto : VendorContactMetadataDto
{
    public Guid ContactTypeId { get; set; }
    public string ContactValue { get; set; } = string.Empty;
    public string? ContactNotes { get; set; }
}
