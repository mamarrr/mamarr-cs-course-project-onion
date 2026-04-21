using App.BLL.CustomerWorkspace.Workspace;
using App.BLL.UnitWorkspace.Workspace;

namespace App.BLL.UnitWorkspace.Units;

public interface IPropertyUnitService
{
    Task<PropertyUnitListResult> ListUnitsAsync(
        PropertyDashboardContext context,
        CancellationToken cancellationToken = default);

    Task<UnitCreateResult> CreateUnitAsync(
        PropertyDashboardContext context,
        UnitCreateRequest request,
        CancellationToken cancellationToken = default);
}
