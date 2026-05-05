using Base.Domain;

namespace App.BLL.Contracts.Contacts;

public class ContactBllDto : BaseEntity
{
    public Guid ManagementCompanyId { get; set; }
    public Guid ContactTypeId { get; set; }
    public string ContactValue { get; set; } = default!;
    public string? Notes { get; set; }
}

