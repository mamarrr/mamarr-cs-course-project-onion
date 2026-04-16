namespace App.BLL.Management;

public interface IManagementLeaseSearchService
{
    Task<ManagementLeasePropertySearchResult> SearchPropertiesAsync(
        ManagementResidentDashboardContext context,
        string? searchTerm,
        CancellationToken cancellationToken = default);

    Task<ManagementLeaseUnitOptionsResult> ListUnitsForPropertyAsync(
        ManagementResidentDashboardContext context,
        Guid propertyId,
        CancellationToken cancellationToken = default);

    Task<ManagementLeaseResidentSearchResult> SearchResidentsAsync(
        ManagementUnitDashboardContext context,
        string? searchTerm,
        CancellationToken cancellationToken = default);

    Task<ManagementLeaseRoleOptionsResult> ListLeaseRolesAsync(
        CancellationToken cancellationToken = default);
}
