using App.BLL.Contracts.ManagementCompanies.Models;

namespace App.BLL.Contracts.ManagementCompanies.Services;

public interface ICompanyMembershipQueryService
{
    Task<CompanyMembershipListResult> ListCompanyMembersAsync(
        CompanyAdminAuthorizedContext context,
        CancellationToken cancellationToken = default);

    Task<CompanyMembershipEditResult> GetMembershipForEditAsync(
        CompanyAdminAuthorizedContext context,
        Guid membershipId,
        CancellationToken cancellationToken = default);
}

