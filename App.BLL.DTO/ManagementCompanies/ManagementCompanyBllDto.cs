using Base.Domain;

namespace App.BLL.Contracts.ManagementCompanies;

public class ManagementCompanyBllDto : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string RegistryCode { get; set; } = default!;
    public string VatNumber { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string Address { get; set; } = default!;
}

public class ManagementCompanyJoinRequestBllDto : BaseEntity
{
    public Guid AppUserId { get; set; }
    public Guid ManagementCompanyId { get; set; }
    public Guid RequestedRoleId { get; set; }
    public Guid StatusId { get; set; }
    public string? Message { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedByAppUserId { get; set; }
}

