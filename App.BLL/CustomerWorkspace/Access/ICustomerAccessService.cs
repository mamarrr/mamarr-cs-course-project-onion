using App.BLL.CustomerWorkspace.Workspace;

namespace App.BLL.CustomerWorkspace.Access;

public interface ICustomerAccessService
{
    Task<CustomerWorkspaceAuthorizationResult> AuthorizeAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default);

    Task<CustomerDashboardAccessResult> AuthorizeCustomerContextAsync(
        Guid appUserId,
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken = default);

    Task<CustomerDashboardAccessResult> ResolveDashboardAccessAsync(
        Guid appUserId,
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken = default);
}

