namespace App.BLL.Management;

public interface IManagementCustomerPropertyService
{
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

