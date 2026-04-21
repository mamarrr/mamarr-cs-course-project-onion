using App.BLL.CustomerWorkspace.Workspace;
using App.BLL.UnitWorkspace.Workspace;

namespace App.BLL.UnitWorkspace.Access;

public interface IManagementUnitDashboardService
{
    Task<ManagementUnitDashboardAccessResult> ResolveUnitDashboardContextAsync(
        PropertyDashboardContext context,
        string unitSlug,
        CancellationToken cancellationToken = default);
}
