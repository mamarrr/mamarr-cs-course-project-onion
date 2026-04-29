namespace App.Contracts.DAL.Contacts;

public sealed class ContactCreateDalDto
{
    public Guid ManagementCompanyId { get; init; }
    public Guid ContactTypeId { get; init; }
    public string ContactValue { get; init; } = default!;
    public string? Notes { get; init; }
}
