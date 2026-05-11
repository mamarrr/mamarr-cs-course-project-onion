namespace App.DTO.v1.Portal.Contacts;

public class ResidentContactMetadataDto
{
    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? ValidTo { get; set; }
    public bool Confirmed { get; set; } = true;
    public bool IsPrimary { get; set; }
}

public class AttachExistingResidentContactDto : ResidentContactMetadataDto
{
    public Guid ContactId { get; set; }
}

public class CreateAndAttachResidentContactDto : ResidentContactMetadataDto
{
    public Guid ContactTypeId { get; set; }
    public string ContactValue { get; set; } = string.Empty;
    public string? ContactNotes { get; set; }
}

public class UpdateResidentContactDto : ResidentContactMetadataDto
{
    public Guid ContactId { get; set; }
}

