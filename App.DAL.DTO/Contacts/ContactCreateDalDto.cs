namespace App.DAL.DTO.Contacts;

public class ContactCreateDalDto
{
    public Guid ManagementCompanyId { get; init; }
    public Guid ContactTypeId { get; init; }
    public string ContactValue { get; init; } = default!;
    public string? Notes { get; init; }
}
