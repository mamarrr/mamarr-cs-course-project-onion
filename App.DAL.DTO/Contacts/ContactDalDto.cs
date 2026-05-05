using Base.Domain;

namespace App.DAL.DTO.Contacts;

public class ContactDalDto : BaseEntity
{
    public Guid ManagementCompanyId { get; init; }
    public Guid ContactTypeId { get; init; }
    public string ContactValue { get; init; } = default!;
    public string? Notes { get; init; }
}
