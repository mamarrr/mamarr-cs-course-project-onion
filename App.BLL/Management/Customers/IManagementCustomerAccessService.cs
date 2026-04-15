namespace App.BLL.Management;

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

