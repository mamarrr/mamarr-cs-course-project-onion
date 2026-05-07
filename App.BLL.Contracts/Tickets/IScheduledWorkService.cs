using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.ScheduledWorks;
using App.BLL.DTO.ScheduledWorks.Models;
using Base.BLL.Contracts;
using FluentResults;

namespace App.BLL.Contracts.Tickets;

public interface IScheduledWorkService : IBaseService<ScheduledWorkBllDto>
{
    Task<Result<ScheduledWorkListModel>> ListForTicketAsync(
        TicketRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<ScheduledWorkDetailsModel>> GetDetailsAsync(
        ScheduledWorkRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<ScheduledWorkFormModel>> GetCreateFormAsync(
        TicketRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<ScheduledWorkFormModel>> GetEditFormAsync(
        ScheduledWorkRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<ScheduledWorkBllDto>> ScheduleAsync(
        TicketRoute route,
        ScheduledWorkBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<ScheduledWorkBllDto>> UpdateScheduleAsync(
        ScheduledWorkRoute route,
        ScheduledWorkBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result> StartWorkAsync(
        ScheduledWorkRoute route,
        DateTime realStart,
        CancellationToken cancellationToken = default);

    Task<Result> CompleteWorkAsync(
        ScheduledWorkRoute route,
        DateTime realEnd,
        CancellationToken cancellationToken = default);

    Task<Result> CancelWorkAsync(
        ScheduledWorkRoute route,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        ScheduledWorkRoute route,
        CancellationToken cancellationToken = default);
}
