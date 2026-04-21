using App.BLL.ManagementCompany.Membership;

namespace App.BLL.ManagementCompany.Access;

public interface IManagementAccessService
{
    Task<ManagementAreaAuthorizationResult> AuthorizeManagementAreaAccessAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default);
}

