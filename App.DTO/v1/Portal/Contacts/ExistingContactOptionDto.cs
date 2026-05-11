namespace App.DTO.v1.Portal.Contacts;

public class ExistingContactOptionDto
{
    public Guid ContactId { get; set; }
    public Guid ContactTypeId { get; set; }
    public string ContactValue { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
