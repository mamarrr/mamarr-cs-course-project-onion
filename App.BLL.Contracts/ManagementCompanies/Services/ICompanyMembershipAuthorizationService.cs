using App.BLL.Contracts.ManagementCompanies.Models;
using FluentResults;

namespace App.BLL.Contracts.ManagementCompanies.Services;

public interface ICompanyMembershipAuthorizationService
{
    Task<Result<CompanyMembershipContext>> AuthorizeManagementAreaAccessAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default);

    Task<Result<CompanyAdminAuthorizedContext>> AuthorizeAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default);
}

