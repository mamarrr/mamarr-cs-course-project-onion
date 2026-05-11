namespace App.DTO.v1.Portal.Contacts;

public class ResidentContactMetadataDto
{
    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? ValidTo { get; set; }
    public bool Confirmed { get; set; } = true;
    public bool IsPrimary { get; set; }
}
