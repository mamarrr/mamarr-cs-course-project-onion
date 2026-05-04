using Base.DAL.Contracts;

namespace App.DAL.Contracts.DAL.ManagementCompanies;

public interface IManagementCompanyJoinRequestRepository : IBaseRepository<ManagementCompanyJoinRequestDalDto>
{
    Task<IReadOnlyList<ManagementCompanyJoinRequestDalDto>> PendingByCompanyAsync(
        Guid managementCompanyId,
        Guid pendingStatusId,
        CancellationToken cancellationToken = default);

    Task<ManagementCompanyJoinRequestDalDto?> FindByIdAndCompanyAsync(
        Guid requestId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> HasPendingRequestAsync(
        Guid appUserId,
        Guid managementCompanyId,
        Guid pendingStatusId,
        CancellationToken cancellationToken = default);

    void AddJoinRequest(ManagementCompanyJoinRequestCreateDalDto dto);

    Task<bool> SetStatusAsync(
        Guid requestId,
        Guid managementCompanyId,
        Guid statusId,
        Guid resolvedByAppUserId,
        DateTime resolvedAt,
        CancellationToken cancellationToken = default);
}
