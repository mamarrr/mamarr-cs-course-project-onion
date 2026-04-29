using Base.Contracts;

namespace App.Contracts.DAL.ManagementCompanies;

public sealed class ManagementCompanyDalDto : IBaseEntity
{
    public Guid Id { get; set; }
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public bool IsActive { get; init; }
}
