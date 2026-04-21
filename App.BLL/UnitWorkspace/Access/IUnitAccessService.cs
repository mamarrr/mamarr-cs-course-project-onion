using App.BLL.CustomerWorkspace.Workspace;
using App.BLL.UnitWorkspace.Workspace;

namespace App.BLL.UnitWorkspace.Access;

public interface IUnitAccessService
{
    Task<UnitDashboardAccessResult> ResolveUnitDashboardContextAsync(
        PropertyDashboardContext context,
        string unitSlug,
        CancellationToken cancellationToken = default);
}
