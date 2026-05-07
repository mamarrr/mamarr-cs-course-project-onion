using App.DAL.DTO.ScheduledWorks;
using Base.DAL.Contracts;

namespace App.DAL.Contracts.Repositories;

public interface IScheduledWorkRepository : IBaseRepository<ScheduledWorkDalDto>
{
    Task<IReadOnlyList<ScheduledWorkListItemDalDto>> AllByCompanyAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScheduledWorkListItemDalDto>> AllByTicketAsync(
        Guid ticketId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<ScheduledWorkDetailsDalDto?> FindDetailsAsync(
        Guid scheduledWorkId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<ScheduledWorkDalDto?> FindInCompanyAsync(
        Guid scheduledWorkId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsForTicketAsync(
        Guid ticketId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> HasWorkLogsAsync(
        Guid scheduledWorkId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> VendorBelongsToTicketCompanyAsync(
        Guid vendorId,
        Guid ticketId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> VendorSupportsTicketCategoryAsync(
        Guid vendorId,
        Guid ticketId,
        CancellationToken cancellationToken = default);

    Task<bool> AnyStartedForTicketAsync(
        Guid ticketId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> AnyCompletedForTicketAsync(
        Guid ticketId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteInCompanyAsync(
        Guid scheduledWorkId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);
}
