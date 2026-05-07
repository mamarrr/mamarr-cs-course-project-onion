using App.BLL.DTO.Admin.Users;
using App.DAL.DTO.Admin.Users;

namespace App.BLL.Mappers.Admin;

public class AdminUserBllMapper
{
    public AdminUserSearchDalDto Map(AdminUserSearchDto dto)
    {
        return new AdminUserSearchDalDto
        {
            SearchText = dto.SearchText,
            Email = dto.Email,
            Name = dto.Name,
            LockedOnly = dto.LockedOnly,
            HasSystemAdminRole = dto.HasSystemAdminRole,
            CreatedFrom = dto.CreatedFrom,
            CreatedTo = dto.CreatedTo
        };
    }

    public AdminUserListItemDto Map(AdminUserListItemDalDto dto)
    {
        return new AdminUserListItemDto
        {
            Id = dto.Id,
            Email = dto.Email,
            FullName = dto.FullName,
            CreatedAt = dto.CreatedAt,
            LockoutEnd = dto.LockoutEnd,
            IsLocked = dto.IsLocked,
            HasSystemAdminRole = dto.HasSystemAdminRole
        };
    }

    public AdminUserDetailsDto Map(AdminUserDetailsDalDto dto)
    {
        return new AdminUserDetailsDto
        {
            Id = dto.Id,
            Email = dto.Email,
            FullName = dto.FullName,
            CreatedAt = dto.CreatedAt,
            LockoutEnd = dto.LockoutEnd,
            IsLocked = dto.IsLocked,
            HasSystemAdminRole = dto.HasSystemAdminRole,
            PhoneNumber = dto.PhoneNumber,
            LastLoginAt = dto.LastLoginAt,
            RefreshTokenCount = dto.RefreshTokenCount,
            Roles = dto.Roles.Select(role => new AdminUserRoleDto
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName
            }).ToList(),
            CompanyMemberships = dto.CompanyMemberships.Select(membership => new AdminUserCompanyMembershipDto
            {
                MembershipId = membership.MembershipId,
                CompanyId = membership.CompanyId,
                CompanyName = membership.CompanyName,
                RoleCode = membership.RoleCode,
                RoleLabel = membership.RoleLabel,
                ValidFrom = membership.ValidFrom,
                ValidTo = membership.ValidTo
            }).ToList()
        };
    }
}
