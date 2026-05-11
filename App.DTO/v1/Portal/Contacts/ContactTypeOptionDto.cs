namespace App.DTO.v1.Portal.Contacts;

public class ContactTypeOptionDto
{
    public Guid ContactTypeId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Code { get; set; }
}
