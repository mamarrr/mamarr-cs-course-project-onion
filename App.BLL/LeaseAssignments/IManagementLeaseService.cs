using App.BLL.ResidentWorkspace.Residents;
using App.BLL.UnitWorkspace.Workspace;

namespace App.BLL.LeaseAssignments;

public interface IManagementLeaseService
{
    Task<ManagementResidentLeaseListResult> ListForResidentAsync(
        ResidentDashboardContext context,
        CancellationToken cancellationToken = default);

    Task<ManagementUnitLeaseListResult> ListForUnitAsync(
        ManagementUnitDashboardContext context,
        CancellationToken cancellationToken = default);

    Task<ManagementLeaseDetailsResult> GetForResidentAsync(
        ResidentDashboardContext context,
        Guid leaseId,
        CancellationToken cancellationToken = default);

    Task<ManagementLeaseDetailsResult> GetForUnitAsync(
        ManagementUnitDashboardContext context,
        Guid leaseId,
        CancellationToken cancellationToken = default);

    Task<ManagementLeaseCommandResult> CreateFromResidentAsync(
        ResidentDashboardContext context,
        ManagementLeaseCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<ManagementLeaseCommandResult> CreateFromUnitAsync(
        ManagementUnitDashboardContext context,
        ManagementLeaseCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<ManagementLeaseCommandResult> UpdateFromResidentAsync(
        ResidentDashboardContext context,
        ManagementLeaseUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<ManagementLeaseCommandResult> UpdateFromUnitAsync(
        ManagementUnitDashboardContext context,
        ManagementLeaseUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<ManagementLeaseCommandResult> DeleteFromResidentAsync(
        ResidentDashboardContext context,
        ManagementLeaseDeleteRequest request,
        CancellationToken cancellationToken = default);

    Task<ManagementLeaseCommandResult> DeleteFromUnitAsync(
        ManagementUnitDashboardContext context,
        ManagementLeaseDeleteRequest request,
        CancellationToken cancellationToken = default);
}
