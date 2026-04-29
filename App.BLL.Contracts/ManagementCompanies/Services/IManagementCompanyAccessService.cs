using App.BLL.Contracts.ManagementCompanies.Models;

namespace App.BLL.Contracts.ManagementCompanies.Services;

public interface IManagementCompanyAccessService
{
    Task<CompanyAreaAuthorizationResult> AuthorizeManagementAreaAccessAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default);
}

