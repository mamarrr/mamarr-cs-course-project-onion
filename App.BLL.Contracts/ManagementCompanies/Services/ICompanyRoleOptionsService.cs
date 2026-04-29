using App.BLL.Contracts.ManagementCompanies.Models;

namespace App.BLL.Contracts.ManagementCompanies.Services;

public interface ICompanyRoleOptionsService
{
    Task<IReadOnlyList<CompanyMembershipRoleOption>> GetAddRoleOptionsAsync(
        CompanyAdminAuthorizedContext context,
        CancellationToken cancellationToken = default);

    Task<CompanyMembershipOptionsResult> GetEditRoleOptionsAsync(
        CompanyAdminAuthorizedContext context,
        Guid membershipId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CompanyMembershipRoleOption>> GetAvailableRolesAsync(
        CancellationToken cancellationToken = default);
}

