using App.BLL.ManagementCompany.Membership;
using App.DAL.EF;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Shared.Policies;

public static class CompanyMembershipPolicy
{
    public const string OwnerRoleCode = "OWNER";
    public const string ManagerRoleCode = "MANAGER";

    public static readonly HashSet<string> AdminRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        OwnerRoleCode,
        ManagerRoleCode
    };

    public static readonly HashSet<string> ManagementAreaRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        OwnerRoleCode,
        ManagerRoleCode,
        "FINANCE",
        "SUPPORT"
    };

    public static bool IsOwnerRole(string? roleCode)
    {
        return string.Equals(roleCode, OwnerRoleCode, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsMembershipEffective(bool isActive, DateOnly validFrom, DateOnly? validTo, DateOnly today)
    {
        if (!isActive) return false;
        if (validFrom > today) return false;
        if (validTo.HasValue && validTo.Value < today) return false;
        return true;
    }

    public static bool IsValidDateRange(DateOnly validFrom, DateOnly? validTo)
    {
        return !validTo.HasValue || validTo.Value >= validFrom;
    }

    public static bool CanAssignRoleInGenericFlow(string actorRoleCode, bool isOwner, string roleCode)
    {
        if (IsOwnerRole(roleCode)) return false;
        return isOwner || string.Equals(actorRoleCode, ManagerRoleCode, StringComparison.OrdinalIgnoreCase);
    }

    public static CompanyMembershipRoleOption MapRoleOption(ManagementCompanyRole role)
    {
        return new CompanyMembershipRoleOption
        {
            RoleId = role.Id,
            RoleCode = role.Code,
            RoleLabel = role.Label.ToString()
        };
    }

    public static async Task<int> CountEffectiveOwnersAsync(AppDbContext dbContext, Guid companyId, CancellationToken cancellationToken)
    {
        var ownerRoleIds = await dbContext.ManagementCompanyRoles
            .AsNoTracking()
            .Where(r => r.Code == OwnerRoleCode)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await dbContext.ManagementCompanyUsers
            .AsNoTracking()
            .Where(m => m.ManagementCompanyId == companyId
                        && ownerRoleIds.Contains(m.ManagementCompanyRoleId)
                        && m.IsActive
                        && m.ValidFrom <= today
                        && (!m.ValidTo.HasValue || m.ValidTo >= today))
            .CountAsync(cancellationToken);
    }
}

