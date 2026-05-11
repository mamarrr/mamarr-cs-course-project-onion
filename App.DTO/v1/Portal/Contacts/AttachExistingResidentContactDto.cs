namespace App.DTO.v1.Portal.Contacts;

public class AttachExistingResidentContactDto : ResidentContactMetadataDto
{
    public Guid ContactId { get; set; }
}
