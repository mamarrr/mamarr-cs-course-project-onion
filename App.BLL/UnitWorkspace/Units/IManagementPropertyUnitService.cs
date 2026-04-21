using App.BLL.CustomerWorkspace.Workspace;
using App.BLL.UnitWorkspace.Workspace;

namespace App.BLL.UnitWorkspace.Units;

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
