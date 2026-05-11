namespace App.DTO.v1.Portal.VendorContacts;

public class ExistingVendorContactOptionDto
{
    public Guid ContactId { get; set; }
    public Guid ContactTypeId { get; set; }
    public string ContactValue { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string Label { get; set; } = string.Empty;
}
