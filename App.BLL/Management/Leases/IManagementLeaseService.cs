namespace App.BLL.Management;

public interface IManagementLeaseService
{
    Task<ManagementResidentLeaseListResult> ListForResidentAsync(
        ManagementResidentDashboardContext context,
        CancellationToken cancellationToken = default);

    Task<ManagementUnitLeaseListResult> ListForUnitAsync(
        ManagementUnitDashboardContext context,
        CancellationToken cancellationToken = default);

    Task<ManagementLeaseDetailsResult> GetForResidentAsync(
        ManagementResidentDashboardContext context,
        Guid leaseId,
        CancellationToken cancellationToken = default);

    Task<ManagementLeaseDetailsResult> GetForUnitAsync(
        ManagementUnitDashboardContext context,
        Guid leaseId,
        CancellationToken cancellationToken = default);

    Task<ManagementLeaseCommandResult> CreateFromResidentAsync(
        ManagementResidentDashboardContext context,
        ManagementLeaseCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<ManagementLeaseCommandResult> CreateFromUnitAsync(
        ManagementUnitDashboardContext context,
        ManagementLeaseCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<ManagementLeaseCommandResult> UpdateFromResidentAsync(
        ManagementResidentDashboardContext context,
        ManagementLeaseUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<ManagementLeaseCommandResult> UpdateFromUnitAsync(
        ManagementUnitDashboardContext context,
        ManagementLeaseUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<ManagementLeaseCommandResult> DeleteFromResidentAsync(
        ManagementResidentDashboardContext context,
        ManagementLeaseDeleteRequest request,
        CancellationToken cancellationToken = default);

    Task<ManagementLeaseCommandResult> DeleteFromUnitAsync(
        ManagementUnitDashboardContext context,
        ManagementLeaseDeleteRequest request,
        CancellationToken cancellationToken = default);
}
