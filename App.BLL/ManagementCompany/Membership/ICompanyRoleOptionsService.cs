using App.Domain;

namespace App.BLL.ManagementCompany.Membership;

public interface ICompanyRoleOptionsService
{
    Task<IReadOnlyList<CompanyMembershipRoleOption>> GetAddRoleOptionsAsync(
        CompanyAdminAuthorizedContext context,
        CancellationToken cancellationToken = default);

    Task<CompanyMembershipOptionsResult> GetEditRoleOptionsAsync(
        CompanyAdminAuthorizedContext context,
        Guid membershipId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ManagementCompanyRole>> GetAvailableRolesAsync(
        CancellationToken cancellationToken = default);
}

