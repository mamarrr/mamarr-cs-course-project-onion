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

public class VendorContactEditModelDto
{
    public string CompanySlug { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public Guid VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public VendorContactDto Contact { get; set; } = new();
    public IReadOnlyList<ExistingVendorContactOptionDto> ExistingContactOptions { get; set; } = [];
    public IReadOnlyList<VendorContactTypeOptionDto> ContactTypeOptions { get; set; } = [];
}

public class VendorContactDto
{
    public Guid VendorContactId { get; set; }
    public Guid VendorId { get; set; }
    public Guid ContactId { get; set; }
    public Guid ContactTypeId { get; set; }
    public string ContactTypeCode { get; set; } = string.Empty;
    public string ContactTypeLabel { get; set; } = string.Empty;
    public string ContactValue { get; set; } = string.Empty;
    public string? ContactNotes { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool Confirmed { get; set; }
    public bool IsPrimary { get; set; }
    public string? FullName { get; set; }
    public string? RoleTitle { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Path { get; set; } = string.Empty;
}

public class ExistingVendorContactOptionDto
{
    public Guid ContactId { get; set; }
    public Guid ContactTypeId { get; set; }
    public string ContactValue { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class VendorContactTypeOptionDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Code { get; set; }
}
