namespace App.DTO.v1.Portal.Contacts;

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
