namespace App.BLL.ManagementCompany.Membership;

public interface IManagementUserAuthorizationService
{
    Task<ManagementAreaAuthorizationResult> AuthorizeManagementAreaAccessAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default);

    Task<ManagementUserAdminAuthorizationResult> AuthorizeAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default);
}

