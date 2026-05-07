namespace App.DAL.DTO.Admin.Users;

public class AdminUserSearchDalDto
{
    public string? SearchText { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public bool LockedOnly { get; set; }
    public bool HasSystemAdminRole { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
}

public class AdminUserListItemDalDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public bool IsLocked { get; set; }
    public bool HasSystemAdminRole { get; set; }
}

public class AdminUserDetailsDalDto : AdminUserListItemDalDto
{
    public string? PhoneNumber { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int RefreshTokenCount { get; set; }
    public IReadOnlyList<AdminUserRoleDalDto> Roles { get; set; } = [];
    public IReadOnlyList<AdminUserCompanyMembershipDalDto> CompanyMemberships { get; set; } = [];
}

public class AdminUserRoleDalDto
{
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
}

public class AdminUserCompanyMembershipDalDto
{
    public Guid MembershipId { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public string RoleLabel { get; set; } = string.Empty;
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}
