using App.BLL.Contracts.Properties.Models;
using App.BLL.UnitWorkspace.Workspace;

namespace App.BLL.UnitWorkspace.Units;

public interface IPropertyUnitService
{
    Task<PropertyUnitListResult> ListUnitsAsync(
        PropertyWorkspaceModel context,
        CancellationToken cancellationToken = default);

    Task<UnitCreateResult> CreateUnitAsync(
        PropertyWorkspaceModel context,
        UnitCreateRequest request,
        CancellationToken cancellationToken = default);
}
