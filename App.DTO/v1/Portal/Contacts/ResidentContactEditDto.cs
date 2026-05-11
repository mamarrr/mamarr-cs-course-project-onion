namespace App.DTO.v1.Portal.Contacts;

public class ResidentContactEditDto
{
    public string CompanySlug { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public Guid ResidentId { get; set; }
    public string ResidentIdCode { get; set; } = string.Empty;
    public string ResidentName { get; set; } = string.Empty;
    public ResidentContactDto Contact { get; set; } = new();
    public ResidentContactAssignmentDto Form { get; set; } = new();
    public IReadOnlyList<ExistingContactOptionDto> ExistingContactOptions { get; set; } =
        Array.Empty<ExistingContactOptionDto>();
    public IReadOnlyList<ContactTypeOptionDto> ContactTypeOptions { get; set; } =
        Array.Empty<ContactTypeOptionDto>();
}
