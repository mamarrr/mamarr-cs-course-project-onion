namespace App.BLL.Management;

public interface IManagementUnitDashboardService
{
    Task<ManagementUnitDashboardAccessResult> ResolveUnitDashboardContextAsync(
        ManagementCustomerPropertyDashboardContext context,
        string unitSlug,
        CancellationToken cancellationToken = default);
}
