using Base.Contracts;

namespace App.DAL.DTO.Contacts;

public class ContactDalDto : IBaseEntity
{
    public Guid Id { get; set; }
    public Guid ManagementCompanyId { get; init; }
    public Guid ContactTypeId { get; init; }
    public string ContactValue { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
    public string? Notes { get; init; }
}
