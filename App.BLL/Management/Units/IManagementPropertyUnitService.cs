namespace App.BLL.Management;

public interface IManagementPropertyUnitService
{
    Task<ManagementPropertyUnitListResult> ListUnitsAsync(
        ManagementCustomerPropertyDashboardContext context,
        CancellationToken cancellationToken = default);

    Task<ManagementPropertyUnitCreateResult> CreateUnitAsync(
        ManagementCustomerPropertyDashboardContext context,
        ManagementPropertyUnitCreateRequest request,
        CancellationToken cancellationToken = default);
}
