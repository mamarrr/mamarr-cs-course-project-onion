namespace App.DTO.v1.Portal.Contacts;

public class CreateAndAttachResidentContactDto : ResidentContactMetadataDto
{
    public Guid ContactTypeId { get; set; }
    public string ContactValue { get; set; } = string.Empty;
    public string? ContactNotes { get; set; }
}
