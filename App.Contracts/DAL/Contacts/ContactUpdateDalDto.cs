namespace App.Contracts.DAL.Contacts;

public class ContactUpdateDalDto
{
    public Guid Id { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public Guid ContactTypeId { get; init; }
    public string ContactValue { get; init; } = default!;
    public string? Notes { get; init; }
}
