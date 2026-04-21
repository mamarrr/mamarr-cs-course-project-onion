using App.BLL.ResidentWorkspace.Residents;
using App.BLL.UnitWorkspace.Workspace;

namespace App.BLL.LeaseAssignments;

public interface ILeaseAssignmentService
{
    Task<ResidentLeaseListResult> ListForResidentAsync(
        ResidentDashboardContext context,
        CancellationToken cancellationToken = default);

    Task<UnitLeaseListResult> ListForUnitAsync(
        UnitDashboardContext context,
        CancellationToken cancellationToken = default);

    Task<LeaseDetailsResult> GetForResidentAsync(
        ResidentDashboardContext context,
        Guid leaseId,
        CancellationToken cancellationToken = default);

    Task<LeaseDetailsResult> GetForUnitAsync(
        UnitDashboardContext context,
        Guid leaseId,
        CancellationToken cancellationToken = default);

    Task<LeaseCommandResult> CreateFromResidentAsync(
        ResidentDashboardContext context,
        LeaseCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<LeaseCommandResult> CreateFromUnitAsync(
        UnitDashboardContext context,
        LeaseCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<LeaseCommandResult> UpdateFromResidentAsync(
        ResidentDashboardContext context,
        LeaseUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<LeaseCommandResult> UpdateFromUnitAsync(
        UnitDashboardContext context,
        LeaseUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<LeaseCommandResult> DeleteFromResidentAsync(
        ResidentDashboardContext context,
        LeaseDeleteRequest request,
        CancellationToken cancellationToken = default);

    Task<LeaseCommandResult> DeleteFromUnitAsync(
        UnitDashboardContext context,
        LeaseDeleteRequest request,
        CancellationToken cancellationToken = default);
}
