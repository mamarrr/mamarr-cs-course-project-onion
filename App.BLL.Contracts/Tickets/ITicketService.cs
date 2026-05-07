using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.ScheduledWorks;
using App.BLL.DTO.ScheduledWorks.Models;
using App.BLL.DTO.Tickets;
using App.BLL.DTO.Tickets.Models;
using App.BLL.DTO.WorkLogs;
using App.BLL.DTO.WorkLogs.Models;
using Base.BLL.Contracts;
using FluentResults;

namespace App.BLL.Contracts.Tickets;

public interface ITicketService : IBaseService<TicketBllDto>
{
    Task<Result<ManagementTicketsModel>> SearchAsync(
        ManagementTicketSearchRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<ManagementTicketDetailsModel>> GetDetailsAsync(
        TicketRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<ManagementTicketFormModel>> GetCreateFormAsync(
        TicketSelectorOptionsRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<ManagementTicketFormModel>> GetEditFormAsync(
        TicketRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<TicketSelectorOptionsModel>> GetSelectorOptionsAsync(
        TicketSelectorOptionsRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<TicketBllDto>> CreateAsync(
        ManagementCompanyRoute route,
        TicketBllDto dto,
        CancellationToken cancellationToken = default);
    

    Task<Result<TicketBllDto>> UpdateAsync(
        TicketRoute route,
        TicketBllDto dto,
        CancellationToken cancellationToken = default);
    

    Task<Result> DeleteAsync(
        TicketRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<TicketBllDto>> AdvanceStatusAsync(
        TicketRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<ScheduledWorkListModel>> ListScheduledWorkForTicketAsync(
        TicketRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<ScheduledWorkDetailsModel>> GetScheduledWorkDetailsAsync(
        ScheduledWorkRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<ScheduledWorkFormModel>> GetScheduleCreateFormAsync(
        TicketRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<ScheduledWorkFormModel>> GetScheduleEditFormAsync(
        ScheduledWorkRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<ScheduledWorkBllDto>> ScheduleWorkAsync(
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

    Task<Result> DeleteScheduledWorkAsync(
        ScheduledWorkRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<WorkLogListModel>> ListWorkLogsForScheduledWorkAsync(
        ScheduledWorkRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<WorkLogFormModel>> GetWorkLogCreateFormAsync(
        ScheduledWorkRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<WorkLogFormModel>> GetWorkLogEditFormAsync(
        WorkLogRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<WorkLogDeleteModel>> GetWorkLogDeleteModelAsync(
        WorkLogRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<WorkLogBllDto>> AddWorkLogAsync(
        ScheduledWorkRoute route,
        WorkLogBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<WorkLogBllDto>> UpdateWorkLogAsync(
        WorkLogRoute route,
        WorkLogBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteWorkLogAsync(
        WorkLogRoute route,
        CancellationToken cancellationToken = default);
}
