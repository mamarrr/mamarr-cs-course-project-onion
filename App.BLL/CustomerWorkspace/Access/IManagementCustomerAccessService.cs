using App.BLL.CustomerWorkspace.Workspace;

namespace App.BLL.CustomerWorkspace.Access;

public interface IManagementCustomerAccessService
{
    Task<ManagementCustomersAuthorizationResult> AuthorizeAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default);

    Task<ManagementCustomerDashboardAccessResult> AuthorizeCustomerContextAsync(
        Guid appUserId,
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken = default);

    Task<ManagementCustomerDashboardAccessResult> ResolveDashboardAccessAsync(
        Guid appUserId,
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken = default);
}

