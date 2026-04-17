namespace App.BLL.Management;

public interface IManagementAccessService
{
    Task<ManagementAreaAuthorizationResult> AuthorizeManagementAreaAccessAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default);
}

