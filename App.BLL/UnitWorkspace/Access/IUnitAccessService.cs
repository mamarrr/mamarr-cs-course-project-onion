using App.BLL.Contracts.Properties.Models;
using App.BLL.UnitWorkspace.Workspace;

namespace App.BLL.UnitWorkspace.Access;

public interface IUnitAccessService
{
    Task<UnitDashboardAccessResult> ResolveUnitDashboardContextAsync(
        PropertyWorkspaceModel context,
        string unitSlug,
        CancellationToken cancellationToken = default);
}
