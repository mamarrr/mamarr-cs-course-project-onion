namespace App.DTO.v1.Portal.Contacts;

public class ResidentContactListDto
{
    public string CompanySlug { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public Guid ResidentId { get; set; }
    public string ResidentIdCode { get; set; } = string.Empty;
    public string ResidentName { get; set; } = string.Empty;
    public IReadOnlyList<ResidentContactDto> Contacts { get; set; } = Array.Empty<ResidentContactDto>();
    public IReadOnlyList<ExistingContactOptionDto> ExistingContactOptions { get; set; } =
        Array.Empty<ExistingContactOptionDto>();
    public IReadOnlyList<ContactTypeOptionDto> ContactTypeOptions { get; set; } =
        Array.Empty<ContactTypeOptionDto>();
}

public class ResidentContactEditDto
{
    public string CompanySlug { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public Guid ResidentId { get; set; }
    public string ResidentIdCode { get; set; } = string.Empty;
    public string ResidentName { get; set; } = string.Empty;
    public ResidentContactDto Contact { get; set; } = new();
    public UpdateResidentContactDto Form { get; set; } = new();
    public IReadOnlyList<ExistingContactOptionDto> ExistingContactOptions { get; set; } =
        Array.Empty<ExistingContactOptionDto>();
    public IReadOnlyList<ContactTypeOptionDto> ContactTypeOptions { get; set; } =
        Array.Empty<ContactTypeOptionDto>();
}

public class ResidentContactDto
{
    public Guid ResidentContactId { get; set; }
    public Guid ResidentId { get; set; }
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
    public DateTime CreatedAt { get; set; }
    public string Path { get; set; } = string.Empty;
}

public class ExistingContactOptionDto
{
    public Guid ContactId { get; set; }
    public Guid ContactTypeId { get; set; }
    public string ContactValue { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class ContactTypeOptionDto
{
    public Guid ContactTypeId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Code { get; set; }
}

