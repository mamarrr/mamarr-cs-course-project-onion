using Base.Contracts;

namespace App.Contracts.DAL.Properties;

public sealed class PropertyDalDto : IBaseEntity
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ManagementCompanyId { get; set; }
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public bool IsActive { get; set; }
}
