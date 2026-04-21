namespace App.BLL.ManagementCompany.Membership;

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

