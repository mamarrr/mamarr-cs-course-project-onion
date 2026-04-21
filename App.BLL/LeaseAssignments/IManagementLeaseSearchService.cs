using App.BLL.ResidentWorkspace.Residents;
using App.BLL.UnitWorkspace.Workspace;

namespace App.BLL.LeaseAssignments;

public interface IManagementLeaseSearchService
{
    Task<ManagementLeasePropertySearchResult> SearchPropertiesAsync(
        ResidentDashboardContext context,
        string? searchTerm,
        CancellationToken cancellationToken = default);

    Task<ManagementLeaseUnitOptionsResult> ListUnitsForPropertyAsync(
        ResidentDashboardContext context,
        Guid propertyId,
        CancellationToken cancellationToken = default);

    Task<ManagementLeaseResidentSearchResult> SearchResidentsAsync(
        UnitDashboardContext context,
        string? searchTerm,
        CancellationToken cancellationToken = default);

    Task<ManagementLeaseRoleOptionsResult> ListLeaseRolesAsync(
        CancellationToken cancellationToken = default);
}
