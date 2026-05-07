using App.DAL.DTO.WorkLogs;
using Base.DAL.Contracts;

namespace App.DAL.Contracts.Repositories;

public interface IWorkLogRepository : IBaseRepository<WorkLogDalDto>
{
    Task<IReadOnlyList<WorkLogListItemDalDto>> AllByScheduledWorkAsync(
        Guid scheduledWorkId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<WorkLogDalDto?> FindInCompanyAsync(
        Guid workLogId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsInCompanyAsync(
        Guid workLogId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsForScheduledWorkAsync(
        Guid scheduledWorkId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsForTicketAsync(
        Guid ticketId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<WorkLogTotalsDalDto> TotalsForScheduledWorkAsync(
        Guid scheduledWorkId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<WorkLogTotalsDalDto> TotalsForTicketAsync(
        Guid ticketId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteInCompanyAsync(
        Guid workLogId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);
}
