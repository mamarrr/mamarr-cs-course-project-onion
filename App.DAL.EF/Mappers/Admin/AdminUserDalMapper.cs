using App.DAL.DTO.Admin.Users;
using App.Domain;
using App.Domain.Identity;

namespace App.DAL.EF.Mappers.Admin;

public class AdminUserDalMapper
{
    public AdminUserListItemDalDto Map(AppUser user, bool hasSystemAdminRole)
    {
        var lockoutEnd = user.LockoutEnd?.UtcDateTime;
        return new AdminUserListItemDalDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            CreatedAt = user.CreatedAt,
            LockoutEnd = lockoutEnd,
            IsLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow,
            HasSystemAdminRole = hasSystemAdminRole
        };
    }

    public AdminUserCompanyMembershipDalDto Map(ManagementCompanyUser membership)
    {
        return new AdminUserCompanyMembershipDalDto
        {
            MembershipId = membership.Id,
            CompanyId = membership.ManagementCompanyId,
            CompanyName = membership.ManagementCompany?.Name ?? string.Empty,
            RoleCode = membership.ManagementCompanyRole?.Code ?? string.Empty,
            RoleLabel = membership.ManagementCompanyRole?.Label.ToString() ?? string.Empty,
            ValidFrom = membership.ValidFrom,
            ValidTo = membership.ValidTo
        };
    }
}
