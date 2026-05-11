namespace App.DTO.v1.Portal.VendorContacts;

public class VendorContactListDto
{
    public string CompanySlug { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public Guid VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public IReadOnlyList<VendorContactDto> Contacts { get; set; } = [];
    public IReadOnlyList<ExistingVendorContactOptionDto> ExistingContactOptions { get; set; } = [];
    public IReadOnlyList<VendorContactTypeOptionDto> ContactTypeOptions { get; set; } = [];
}
