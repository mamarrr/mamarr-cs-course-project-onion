using App.BLL.ManagementCompany.Membership;

namespace App.BLL.ManagementCompany.Access;

public interface IManagementCompanyAccessService
{
    Task<CompanyAreaAuthorizationResult> AuthorizeManagementAreaAccessAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default);
}

