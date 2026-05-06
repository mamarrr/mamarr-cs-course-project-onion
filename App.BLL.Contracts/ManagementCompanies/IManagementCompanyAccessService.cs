using App.BLL.DTO.ManagementCompanies.Models;
using FluentResults;

namespace App.BLL.Contracts.ManagementCompanies;

public interface IManagementCompanyAccessService
{
    Task<Result<CompanyMembershipContext>> AuthorizeManagementAreaAccessAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default);
}

