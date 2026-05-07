namespace App.BLL.DTO.Admin.Users;

public class AdminUserSearchDto
{
    public string? SearchText { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public bool LockedOnly { get; set; }
    public bool HasSystemAdminRole { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
}

public class AdminUserListDto
{
    public AdminUserSearchDto Search { get; set; } = new();
    public IReadOnlyList<AdminUserListItemDto> Users { get; set; } = [];
}

public class AdminUserListItemDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public bool IsLocked { get; set; }
    public bool HasSystemAdminRole { get; set; }
}

public class AdminUserDetailsDto : AdminUserListItemDto
{
    public string? PhoneNumber { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int RefreshTokenCount { get; set; }
    public IReadOnlyList<AdminUserRoleDto> Roles { get; set; } = [];
    public IReadOnlyList<AdminUserCompanyMembershipDto> CompanyMemberships { get; set; } = [];
}

public class AdminUserRoleDto
{
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
}

public class AdminUserCompanyMembershipDto
{
    public Guid MembershipId { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public string RoleLabel { get; set; } = string.Empty;
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}
