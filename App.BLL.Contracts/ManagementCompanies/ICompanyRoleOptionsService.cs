using App.BLL.Contracts.ManagementCompanies.Models;
using FluentResults;

namespace App.BLL.Contracts.ManagementCompanies;

public interface ICompanyRoleOptionsService
{
    Task<IReadOnlyList<CompanyMembershipRoleOption>> GetAddRoleOptionsAsync(
        CompanyAdminAuthorizedContext context,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CompanyMembershipRoleOption>>> GetEditRoleOptionsAsync(
        CompanyAdminAuthorizedContext context,
        Guid membershipId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CompanyMembershipRoleOption>> GetAvailableRolesAsync(
        CancellationToken cancellationToken = default);
}

