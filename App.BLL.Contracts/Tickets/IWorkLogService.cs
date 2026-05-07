using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.WorkLogs;
using App.BLL.DTO.WorkLogs.Models;
using Base.BLL.Contracts;
using FluentResults;

namespace App.BLL.Contracts.Tickets;

public interface IWorkLogService : IBaseService<WorkLogBllDto>
{
    Task<Result<WorkLogListModel>> ListForScheduledWorkAsync(
        ScheduledWorkRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<WorkLogFormModel>> GetCreateFormAsync(
        ScheduledWorkRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<WorkLogFormModel>> GetEditFormAsync(
        WorkLogRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<WorkLogDeleteModel>> GetDeleteModelAsync(
        WorkLogRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<WorkLogBllDto>> AddAsync(
        ScheduledWorkRoute route,
        WorkLogBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<WorkLogBllDto>> UpdateAsync(
        WorkLogRoute route,
        WorkLogBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        WorkLogRoute route,
        CancellationToken cancellationToken = default);
}
