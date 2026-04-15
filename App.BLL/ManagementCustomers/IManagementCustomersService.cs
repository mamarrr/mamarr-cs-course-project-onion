namespace App.BLL.ManagementCustomers;

public interface IManagementCustomersService
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

    Task<ManagementCustomerListResult> ListAsync(
        ManagementCustomersAuthorizedContext context,
        CancellationToken cancellationToken = default);

    Task<ManagementCustomerCreateResult> CreateAsync(
        ManagementCustomersAuthorizedContext context,
        ManagementCustomerCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<ManagementCustomerPropertyListResult> ListPropertiesAsync(
        ManagementCustomerDashboardContext context,
        CancellationToken cancellationToken = default);

    Task<ManagementCustomerPropertyCreateResult> CreatePropertyAsync(
        ManagementCustomerDashboardContext context,
        ManagementCustomerPropertyCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<ManagementCustomerPropertyDashboardAccessResult> ResolvePropertyDashboardContextAsync(
        ManagementCustomerDashboardContext context,
        string propertySlug,
        CancellationToken cancellationToken = default);
}
