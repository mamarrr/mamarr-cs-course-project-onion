using App.BLL.ResidentWorkspace.Residents;
using App.BLL.UnitWorkspace.Workspace;

namespace App.BLL.LeaseAssignments;

public interface ILeaseLookupService
{
    Task<LeasePropertySearchResult> SearchPropertiesAsync(
        ResidentDashboardContext context,
        string? searchTerm,
        CancellationToken cancellationToken = default);

    Task<LeaseUnitOptionsResult> ListUnitsForPropertyAsync(
        ResidentDashboardContext context,
        Guid propertyId,
        CancellationToken cancellationToken = default);

    Task<LeaseResidentSearchResult> SearchResidentsAsync(
        UnitDashboardContext context,
        string? searchTerm,
        CancellationToken cancellationToken = default);

    Task<LeaseRoleOptionsResult> ListLeaseRolesAsync(
        CancellationToken cancellationToken = default);
}
