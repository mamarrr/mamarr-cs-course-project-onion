using Base.Domain;

namespace App.DAL.DTO.ManagementCompanies;

public class ManagementCompanyDalDto : BaseEntity
{
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string RegistryCode { get; init; } = default!;
    public string VatNumber { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string Phone { get; init; } = default!;
    public string Address { get; init; } = default!;
}

public class ManagementCompanyContextDalDto
{
    public Guid ManagementCompanyId { get; init; }
    public string Slug { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public Guid MembershipId { get; init; }
    public Guid RoleId { get; init; }
    public string RoleCode { get; init; } = default!;
    
    public DateOnly ValidFrom { get; init; }
    public DateOnly? ValidTo { get; init; }
}

public class ManagementCompanyProfileDalDto : BaseEntity
{
    public string Slug { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string RegistryCode { get; init; } = default!;
    public string VatNumber { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string Phone { get; init; } = default!;
    public string Address { get; init; } = default!;
    
}

public class ManagementCompanyMembershipDalDto : BaseEntity
{
    public Guid ManagementCompanyId { get; init; }
    public Guid AppUserId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public Guid RoleId { get; init; }
    public string RoleCode { get; init; } = default!;
    public string RoleLabel { get; init; } = default!;
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string JobTitle { get; init; } = default!;
    
    public DateOnly ValidFrom { get; init; }
    public DateOnly? ValidTo { get; init; }
}

public class ManagementCompanyMembershipCreateDalDto
{
    public Guid Id { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public Guid AppUserId { get; init; }
    public Guid RoleId { get; init; }
    public string JobTitle { get; init; } = default!;
    
    public DateOnly ValidFrom { get; init; }
    public DateOnly? ValidTo { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class ManagementCompanyMembershipUpdateDalDto
{
    public Guid MembershipId { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public Guid RoleId { get; init; }
    public string JobTitle { get; init; } = default!;
    
    public DateOnly ValidFrom { get; init; }
    public DateOnly? ValidTo { get; init; }
}

public class ManagementCompanyJoinRequestDalDto : BaseEntity
{
    public Guid AppUserId { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public Guid RequestedRoleId { get; init; }
    public Guid StatusId { get; init; }
    public string? Message { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public Guid? ResolvedByAppUserId { get; init; }
}

public class ManagementCompanyJoinRequestDetailsDalDto : BaseEntity
{
    public Guid AppUserId { get; init; }
    public string RequesterFirstName { get; init; } = default!;
    public string RequesterLastName { get; init; } = default!;
    public string RequesterEmail { get; init; } = default!;
    public Guid ManagementCompanyId { get; init; }
    public Guid RequestedRoleId { get; init; }
    public string RequestedRoleCode { get; init; } = default!;
    public string RequestedRoleLabel { get; init; } = default!;
    public Guid StatusId { get; init; }
    public string StatusCode { get; init; } = default!;
    public string StatusLabel { get; init; } = default!;
    public string? Message { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public Guid? ResolvedByAppUserId { get; init; }
}
