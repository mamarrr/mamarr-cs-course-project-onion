using App.BLL.ResidentWorkspace.Residents;
using App.BLL.UnitWorkspace.Workspace;

namespace App.BLL.LeaseAssignments;

public interface IManagementLeaseService
{
    Task<ManagementResidentLeaseListResult> ListForResidentAsync(
        ResidentDashboardContext context,
        CancellationToken cancellationToken = default);

    Task<ManagementUnitLeaseListResult> ListForUnitAsync(
        UnitDashboardContext context,
        CancellationToken cancellationToken = default);

    Task<ManagementLeaseDetailsResult> GetForResidentAsync(
        ResidentDashboardContext context,
        Guid leaseId,
        CancellationToken cancellationToken = default);

    Task<ManagementLeaseDetailsResult> GetForUnitAsync(
        UnitDashboardContext context,
        Guid leaseId,
        CancellationToken cancellationToken = default);

    Task<ManagementLeaseCommandResult> CreateFromResidentAsync(
        ResidentDashboardContext context,
        ManagementLeaseCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<ManagementLeaseCommandResult> CreateFromUnitAsync(
        UnitDashboardContext context,
        ManagementLeaseCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<ManagementLeaseCommandResult> UpdateFromResidentAsync(
        ResidentDashboardContext context,
        ManagementLeaseUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<ManagementLeaseCommandResult> UpdateFromUnitAsync(
        UnitDashboardContext context,
        ManagementLeaseUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<ManagementLeaseCommandResult> DeleteFromResidentAsync(
        ResidentDashboardContext context,
        ManagementLeaseDeleteRequest request,
        CancellationToken cancellationToken = default);

    Task<ManagementLeaseCommandResult> DeleteFromUnitAsync(
        UnitDashboardContext context,
        ManagementLeaseDeleteRequest request,
        CancellationToken cancellationToken = default);
}
