using App.BLL.Contracts.ManagementCompanies.Models;

namespace App.BLL.Contracts.ManagementCompanies.Services;

public interface ICompanyMembershipAuthorizationService
{
    Task<CompanyAreaAuthorizationResult> AuthorizeManagementAreaAccessAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default);

    Task<CompanyAdminAuthorizationResult> AuthorizeAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default);
}

