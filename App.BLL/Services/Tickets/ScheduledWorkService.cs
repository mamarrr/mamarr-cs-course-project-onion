using App.BLL.Contracts.Tickets;
using App.BLL.DTO.Common;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Customers.Models;
using App.BLL.DTO.ScheduledWorks;
using App.BLL.DTO.ScheduledWorks.Models;
using App.BLL.DTO.Tickets.Models;
using App.BLL.Mappers.ScheduledWorks;
using App.DAL.Contracts;
using App.DAL.Contracts.Repositories;
using App.DAL.DTO.ScheduledWorks;
using App.DAL.DTO.Tickets;
using Base.BLL;
using FluentResults;

namespace App.BLL.Services.Tickets;

public class ScheduledWorkService :
    BaseService<ScheduledWorkBllDto, ScheduledWorkDalDto, IScheduledWorkRepository, IAppUOW>,
    IScheduledWorkService
{
    private static readonly HashSet<string> ReadAllowedRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "OWNER",
        "MANAGER",
        "FINANCE",
        "SUPPORT"
    };

    private static readonly HashSet<string> WriteAllowedRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "OWNER",
        "MANAGER",
        "SUPPORT"
    };

    private static readonly HashSet<string> DeleteAllowedRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "OWNER",
        "MANAGER"
    };

    private readonly IAppUOW _uow;
    private readonly ScheduledWorkBllDtoMapper _scheduledWorkMapper = new();

    public ScheduledWorkService(
        IAppUOW uow)
        : base(uow.ScheduledWorks, uow, new ScheduledWorkBllDtoMapper())
    {
        _uow = uow;
    }

    public async Task<Result<ScheduledWorkListModel>> ListForTicketAsync(
        TicketRoute route,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveCompanyWorkspaceAsync(
            route.AppUserId,
            route.CompanySlug,
            ReadAllowedRoleCodes,
            cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail<ScheduledWorkListModel>(workspace.Errors);
        }

        var ticket = await _uow.Tickets.FindDetailsAsync(
            route.TicketId,
            workspace.Value.ManagementCompanyId,
            cancellationToken);
        if (ticket is null)
        {
            return Result.Fail<ScheduledWorkListModel>(new NotFoundError(T("TicketNotFound", "Ticket was not found.")));
        }

        var work = await _uow.ScheduledWorks.AllByTicketAsync(
            route.TicketId,
            workspace.Value.ManagementCompanyId,
            cancellationToken);

        return Result.Ok(new ScheduledWorkListModel
        {
            CompanySlug = workspace.Value.CompanySlug,
            CompanyName = workspace.Value.CompanyName,
            TicketId = ticket.Id,
            TicketNr = ticket.TicketNr,
            TicketTitle = ticket.Title,
            Items = work.Select(MapScheduledWorkListItem).ToList()
        });
    }

    public async Task<Result<ScheduledWorkDetailsModel>> GetDetailsAsync(
        ScheduledWorkRoute route,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveCompanyWorkspaceAsync(
            route.AppUserId,
            route.CompanySlug,
            ReadAllowedRoleCodes,
            cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail<ScheduledWorkDetailsModel>(workspace.Errors);
        }

        var details = await _uow.ScheduledWorks.FindDetailsAsync(
            route.ScheduledWorkId,
            workspace.Value.ManagementCompanyId,
            cancellationToken);
        if (details is null || details.TicketId != route.TicketId)
        {
            return Result.Fail<ScheduledWorkDetailsModel>(new NotFoundError(T("ScheduledWorkNotFound", "Scheduled work was not found.")));
        }

        return Result.Ok(MapScheduledWorkDetails(details));
    }

    public async Task<Result<ScheduledWorkFormModel>> GetCreateFormAsync(
        TicketRoute route,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveTicketForScheduledWorkAsync(route, WriteAllowedRoleCodes, cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail<ScheduledWorkFormModel>(context.Errors);
        }

        var plannedStatus = await FindWorkStatusByCodeAsync(WorkWorkflowConstants.Scheduled, cancellationToken);
        return Result.Ok(await BuildScheduledWorkFormAsync(
            context.Value.Workspace,
            context.Value.Ticket,
            null,
            plannedStatus?.Id ?? Guid.Empty,
            cancellationToken));
    }

    public async Task<Result<ScheduledWorkFormModel>> GetEditFormAsync(
        ScheduledWorkRoute route,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveScheduledWorkContextAsync(route, WriteAllowedRoleCodes, cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail<ScheduledWorkFormModel>(context.Errors);
        }

        return Result.Ok(await BuildScheduledWorkFormAsync(
            context.Value.Workspace,
            context.Value.Ticket,
            context.Value.ScheduledWork,
            context.Value.ScheduledWork.WorkStatusId,
            cancellationToken));
    }

    public async Task<Result<ScheduledWorkBllDto>> ScheduleAsync(
        TicketRoute route,
        ScheduledWorkBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveTicketForScheduledWorkAsync(route, WriteAllowedRoleCodes, cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail<ScheduledWorkBllDto>(context.Errors);
        }

        var scheduleState = ValidateTicketStatusForWorkAction(
            context.Value.Ticket.StatusCode,
            TicketWorkflowConstants.Assigned,
            TicketWorkflowConstants.Completed,
            T("TicketMustBeAssignedBeforeSchedulingWork", "Assign the ticket before scheduling work."));
        if (scheduleState.IsFailed)
        {
            return Result.Fail<ScheduledWorkBllDto>(scheduleState.Errors);
        }

        var validation = await ValidateScheduledWorkAsync(
            dto,
            context.Value.Workspace.ManagementCompanyId,
            route.TicketId,
            cancellationToken);
        if (validation.IsFailed)
        {
            return Result.Fail<ScheduledWorkBllDto>(validation.Errors);
        }

        var plannedStatus = await FindWorkStatusByCodeAsync(WorkWorkflowConstants.Scheduled, cancellationToken);
        if (plannedStatus is null)
        {
            return Result.Fail<ScheduledWorkBllDto>(new BusinessRuleError(T("ScheduledWorkStatusMissing", "Scheduled work status is not configured.")));
        }

        var normalized = NormalizeScheduledWork(dto);
        normalized.Id = Guid.Empty;
        normalized.TicketId = route.TicketId;
        normalized.WorkStatusId = plannedStatus.Id;

        var dalDto = _scheduledWorkMapper.Map(normalized);
        if (dalDto is null)
        {
            return Result.Fail<ScheduledWorkBllDto>("Scheduled work mapping failed.");
        }

        Guid id;
        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            id = _uow.ScheduledWorks.Add(dalDto);

            var ticketStatusUpdate = await StageTicketStatusIfImmediateNextAsync(
                context.Value.Ticket,
                TicketWorkflowConstants.Scheduled,
                context.Value.Workspace.ManagementCompanyId,
                cancellationToken);
            if (ticketStatusUpdate.IsFailed)
            {
                await _uow.RollbackTransactionAsync(cancellationToken);
                return Result.Fail<ScheduledWorkBllDto>(ticketStatusUpdate.Errors);
            }

            await _uow.SaveChangesAsync(cancellationToken);
            await _uow.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _uow.RollbackTransactionAsync(cancellationToken);
            return Result.Fail<ScheduledWorkBllDto>(new ConflictError(T(
                "ScheduledWorkCreateFailed",
                "Failed to schedule work due to a data conflict.")));
        }

        var created = await _uow.ScheduledWorks.FindInCompanyAsync(
            id,
            context.Value.Workspace.ManagementCompanyId,
            cancellationToken);

        return created is null
            ? Result.Fail<ScheduledWorkBllDto>(new NotFoundError(T("ScheduledWorkNotFound", "Scheduled work was not found.")))
            : Result.Ok(_scheduledWorkMapper.Map(created)!);
    }

    public async Task<Result<ScheduledWorkBllDto>> UpdateScheduleAsync(
        ScheduledWorkRoute route,
        ScheduledWorkBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveScheduledWorkContextAsync(route, WriteAllowedRoleCodes, cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail<ScheduledWorkBllDto>(context.Errors);
        }

        var validation = await ValidateScheduledWorkAsync(
            dto,
            context.Value.Workspace.ManagementCompanyId,
            route.TicketId,
            cancellationToken);
        if (validation.IsFailed)
        {
            return Result.Fail<ScheduledWorkBllDto>(validation.Errors);
        }

        var normalized = NormalizeScheduledWork(dto);
        normalized.Id = route.ScheduledWorkId;
        normalized.TicketId = route.TicketId;

        var dalDto = _scheduledWorkMapper.Map(normalized);
        if (dalDto is null)
        {
            return Result.Fail<ScheduledWorkBllDto>("Scheduled work mapping failed.");
        }

        var updated = await _uow.ScheduledWorks.UpdateAsync(
            dalDto,
            context.Value.Workspace.ManagementCompanyId,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Ok(_scheduledWorkMapper.Map(updated)!);
    }

    public async Task<Result> StartWorkAsync(
        ScheduledWorkRoute route,
        DateTime realStart,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveScheduledWorkContextAsync(route, WriteAllowedRoleCodes, cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail(context.Errors);
        }

        var startState = ValidateTicketStatusForWorkAction(
            context.Value.Ticket.StatusCode,
            TicketWorkflowConstants.Scheduled,
            TicketWorkflowConstants.Completed,
            T("TicketMustBeScheduledBeforeStartingWork", "Move the ticket to scheduled before starting work."));
        if (startState.IsFailed)
        {
            return Result.Fail(startState.Errors);
        }

        if (realStart == default)
        {
            return Result.Fail(new ValidationAppError("Validation failed.", [
                Failure(nameof(ScheduledWorkBllDto.RealStart), T("RealStartRequired", "Actual start is required."))
            ]));
        }

        var inProgressStatus = await FindWorkStatusByCodeAsync(WorkWorkflowConstants.InProgress, cancellationToken);
        if (inProgressStatus is null)
        {
            return Result.Fail(new BusinessRuleError(T("WorkInProgressStatusMissing", "In-progress work status is not configured.")));
        }

        var dto = context.Value.ScheduledWork;
        dto.RealStart = realStart;
        dto.RealEnd = null;
        dto.WorkStatusId = inProgressStatus.Id;

        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            await _uow.ScheduledWorks.UpdateAsync(
                dto,
                context.Value.Workspace.ManagementCompanyId,
                cancellationToken);

            var ticketStatusUpdate = await StageTicketStatusIfImmediateNextAsync(
                context.Value.Ticket,
                TicketWorkflowConstants.InProgress,
                context.Value.Workspace.ManagementCompanyId,
                cancellationToken);
            if (ticketStatusUpdate.IsFailed)
            {
                await _uow.RollbackTransactionAsync(cancellationToken);
                return Result.Fail(ticketStatusUpdate.Errors);
            }

            await _uow.SaveChangesAsync(cancellationToken);
            await _uow.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _uow.RollbackTransactionAsync(cancellationToken);
            return Result.Fail(new ConflictError(T(
                "ScheduledWorkStartFailed",
                "Failed to start scheduled work due to a data conflict.")));
        }

        return Result.Ok();
    }

    public async Task<Result> CompleteWorkAsync(
        ScheduledWorkRoute route,
        DateTime realEnd,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveScheduledWorkContextAsync(route, WriteAllowedRoleCodes, cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail(context.Errors);
        }

        var completeState = ValidateTicketStatusForWorkAction(
            context.Value.Ticket.StatusCode,
            TicketWorkflowConstants.InProgress,
            TicketWorkflowConstants.Closed,
            T("TicketMustBeInProgressBeforeCompletingWork", "Move the ticket to in progress before completing work."));
        if (completeState.IsFailed)
        {
            return Result.Fail(completeState.Errors);
        }

        if (realEnd == default)
        {
            return Result.Fail(new ValidationAppError("Validation failed.", [
                Failure(nameof(ScheduledWorkBllDto.RealEnd), T("RealEndRequired", "Actual end is required."))
            ]));
        }

        var dto = context.Value.ScheduledWork;
        if (!dto.RealStart.HasValue)
        {
            return Result.Fail(new BusinessRuleError(T("ScheduledWorkMustStartBeforeComplete", "Scheduled work must be started before it can be completed.")));
        }

        if (realEnd < dto.RealStart.Value)
        {
            return Result.Fail(new ValidationAppError("Validation failed.", [
                Failure(nameof(ScheduledWorkBllDto.RealEnd), T("RealEndCannotBeBeforeRealStart", "Actual end cannot be before actual start."))
            ]));
        }

        var completedStatus = await FindWorkStatusByCodeAsync(WorkWorkflowConstants.Done, cancellationToken);
        if (completedStatus is null)
        {
            return Result.Fail(new BusinessRuleError(T("WorkCompletedStatusMissing", "Completed work status is not configured.")));
        }

        dto.RealEnd = realEnd;
        dto.WorkStatusId = completedStatus.Id;

        await _uow.ScheduledWorks.UpdateAsync(
            dto,
            context.Value.Workspace.ManagementCompanyId,
            cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task<Result> CancelWorkAsync(
        ScheduledWorkRoute route,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveScheduledWorkContextAsync(route, WriteAllowedRoleCodes, cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail(context.Errors);
        }

        var cancelledStatus = await FindWorkStatusByCodeAsync(WorkWorkflowConstants.Cancelled, cancellationToken);
        if (cancelledStatus is null)
        {
            return Result.Fail(new BusinessRuleError(T("WorkCancelledStatusMissing", "Cancelled work status is not configured.")));
        }

        var dto = context.Value.ScheduledWork;
        dto.WorkStatusId = cancelledStatus.Id;

        await _uow.ScheduledWorks.UpdateAsync(
            dto,
            context.Value.Workspace.ManagementCompanyId,
            cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(
        ScheduledWorkRoute route,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveScheduledWorkContextAsync(route, DeleteAllowedRoleCodes, cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail(context.Errors);
        }

        var hasLogs = await _uow.ScheduledWorks.HasWorkLogsAsync(
            route.ScheduledWorkId,
            context.Value.Workspace.ManagementCompanyId,
            cancellationToken);
        if (hasLogs)
        {
            return Result.Fail(new BusinessRuleError(T("ScheduledWorkDeleteBlockedByLogs", "Scheduled work cannot be deleted while work logs exist.")));
        }

        var deleted = await _uow.ScheduledWorks.DeleteInCompanyAsync(
            route.ScheduledWorkId,
            context.Value.Workspace.ManagementCompanyId,
            cancellationToken);
        if (!deleted)
        {
            return Result.Fail(new NotFoundError(T("ScheduledWorkNotFound", "Scheduled work was not found.")));
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    private async Task<Result<CompanyWorkspaceModel>> ResolveCompanyWorkspaceAsync(
        Guid userId,
        string companySlug,
        ISet<string> allowedRoleCodes,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return Result.Fail(new UnauthorizedError("Authentication is required."));
        }

        if (string.IsNullOrWhiteSpace(companySlug))
        {
            return Result.Fail(new NotFoundError(App.Resources.Views.UiText.ManagementCompanyWasNotFound));
        }

        var company = await _uow.ManagementCompanies.FirstBySlugAsync(companySlug, cancellationToken);
        if (company is null)
        {
            return Result.Fail(new NotFoundError(App.Resources.Views.UiText.ManagementCompanyWasNotFound));
        }

        var roleCode = await _uow.ManagementCompanies.FindActiveUserRoleCodeAsync(
            userId,
            company.Id,
            cancellationToken);
        if (roleCode is null || !allowedRoleCodes.Contains(roleCode))
        {
            return Result.Fail(new ForbiddenError(App.Resources.Views.UiText.AccessDeniedDescription));
        }

        return Result.Ok(new CompanyWorkspaceModel
        {
            AppUserId = userId,
            ManagementCompanyId = company.Id,
            CompanySlug = company.Slug,
            CompanyName = company.Name,
            RoleCode = roleCode
        });
    }

    private async Task<Result<ScheduledWorkTicketContext>> ResolveTicketForScheduledWorkAsync(
        TicketRoute route,
        ISet<string> allowedRoleCodes,
        CancellationToken cancellationToken)
    {
        var workspace = await ResolveCompanyWorkspaceAsync(
            route.AppUserId,
            route.CompanySlug,
            allowedRoleCodes,
            cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail<ScheduledWorkTicketContext>(workspace.Errors);
        }

        var ticket = await _uow.Tickets.FindDetailsAsync(
            route.TicketId,
            workspace.Value.ManagementCompanyId,
            cancellationToken);
        if (ticket is null)
        {
            return Result.Fail<ScheduledWorkTicketContext>(new NotFoundError(T("TicketNotFound", "Ticket was not found.")));
        }

        return Result.Ok(new ScheduledWorkTicketContext(workspace.Value, ticket));
    }

    private async Task<Result<ScheduledWorkContext>> ResolveScheduledWorkContextAsync(
        ScheduledWorkRoute route,
        ISet<string> allowedRoleCodes,
        CancellationToken cancellationToken)
    {
        var ticketContext = await ResolveTicketForScheduledWorkAsync(route, allowedRoleCodes, cancellationToken);
        if (ticketContext.IsFailed)
        {
            return Result.Fail<ScheduledWorkContext>(ticketContext.Errors);
        }

        var scheduledWork = await _uow.ScheduledWorks.FindInCompanyAsync(
            route.ScheduledWorkId,
            ticketContext.Value.Workspace.ManagementCompanyId,
            cancellationToken);
        if (scheduledWork is null || scheduledWork.TicketId != route.TicketId)
        {
            return Result.Fail<ScheduledWorkContext>(new NotFoundError(T("ScheduledWorkNotFound", "Scheduled work was not found.")));
        }

        return Result.Ok(new ScheduledWorkContext(
            ticketContext.Value.Workspace,
            ticketContext.Value.Ticket,
            scheduledWork));
    }

    private async Task<ScheduledWorkFormModel> BuildScheduledWorkFormAsync(
        CompanyWorkspaceModel workspace,
        TicketDetailsDalDto ticket,
        ScheduledWorkDalDto? scheduledWork,
        Guid workStatusId,
        CancellationToken cancellationToken)
    {
        return new ScheduledWorkFormModel
        {
            CompanySlug = workspace.CompanySlug,
            CompanyName = workspace.CompanyName,
            TicketId = ticket.Id,
            TicketNr = ticket.TicketNr,
            TicketTitle = ticket.Title,
            ScheduledWorkId = scheduledWork?.Id,
            VendorId = scheduledWork?.VendorId ?? ticket.VendorId ?? Guid.Empty,
            WorkStatusId = workStatusId,
            ScheduledStart = scheduledWork?.ScheduledStart ?? DateTime.UtcNow,
            ScheduledEnd = scheduledWork?.ScheduledEnd,
            RealStart = scheduledWork?.RealStart,
            RealEnd = scheduledWork?.RealEnd,
            Notes = scheduledWork?.Notes,
            Vendors = (await _uow.Vendors.OptionsForTicketAsync(
                    workspace.ManagementCompanyId,
                    ticket.TicketCategoryId,
                    cancellationToken))
                .Select(MapOption)
                .ToList(),
            WorkStatuses = (await _uow.Lookups.AllWorkStatusesAsync(cancellationToken))
                .Select(MapOption)
                .ToList()
        };
    }

    private async Task<Result> ValidateScheduledWorkAsync(
        ScheduledWorkBllDto dto,
        Guid managementCompanyId,
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var failures = new List<ValidationFailureModel>();

        if (dto.VendorId == Guid.Empty)
        {
            failures.Add(Failure(nameof(dto.VendorId), T("VendorRequired", "Vendor is required.")));
        }
        else if (!await _uow.ScheduledWorks.VendorBelongsToTicketCompanyAsync(
                     dto.VendorId,
                     ticketId,
                     managementCompanyId,
                     cancellationToken))
        {
            failures.Add(Failure(nameof(dto.VendorId), T("InvalidTicketVendor", "Selected vendor is invalid.")));
        }
        else if (!await _uow.ScheduledWorks.VendorSupportsTicketCategoryAsync(
                     dto.VendorId,
                     ticketId,
                     cancellationToken))
        {
            failures.Add(Failure(nameof(dto.VendorId), T(
                "TicketVendorDoesNotSupportCategory",
                "Selected vendor is not assigned to this ticket category.")));
        }

        if (dto.WorkStatusId == Guid.Empty)
        {
            failures.Add(Failure(nameof(dto.WorkStatusId), T("WorkStatusRequired", "Work status is required.")));
        }
        else if (!await _uow.Lookups.WorkStatusExistsAsync(dto.WorkStatusId, cancellationToken))
        {
            failures.Add(Failure(nameof(dto.WorkStatusId), T("InvalidWorkStatus", "Selected work status is invalid.")));
        }

        if (dto.ScheduledStart == default)
        {
            failures.Add(Failure(nameof(dto.ScheduledStart), T("ScheduledStartRequired", "Scheduled start is required.")));
        }

        if (dto.ScheduledEnd.HasValue && dto.ScheduledEnd.Value < dto.ScheduledStart)
        {
            failures.Add(Failure(nameof(dto.ScheduledEnd), T("ScheduledEndCannotBeBeforeScheduledStart", "Scheduled end cannot be before scheduled start.")));
        }

        if (dto.RealEnd.HasValue && dto.RealStart.HasValue && dto.RealEnd.Value < dto.RealStart.Value)
        {
            failures.Add(Failure(nameof(dto.RealEnd), T("RealEndCannotBeBeforeRealStart", "Actual end cannot be before actual start.")));
        }

        if (!string.IsNullOrWhiteSpace(dto.Notes) && dto.Notes.Trim().Length > 4000)
        {
            failures.Add(Failure(nameof(dto.Notes), T("ScheduledWorkNotesMaxLength", "Notes must be 4000 characters or fewer.")));
        }

        return failures.Count == 0
            ? Result.Ok()
            : Result.Fail(new ValidationAppError("Validation failed.", failures));
    }

    private async Task<Result> StageTicketStatusIfImmediateNextAsync(
        TicketDetailsDalDto ticket,
        string targetStatusCode,
        Guid managementCompanyId,
        CancellationToken cancellationToken)
    {
        var currentIndex = StatusIndex(ticket.StatusCode);
        var targetIndex = StatusIndex(targetStatusCode);
        if (currentIndex < 0 || targetIndex < 0 || currentIndex >= targetIndex || targetIndex != currentIndex + 1)
        {
            return Result.Ok();
        }

        var targetStatus = await _uow.Lookups.FindTicketStatusByCodeAsync(targetStatusCode, cancellationToken);
        if (targetStatus is null)
        {
            return Result.Fail(new BusinessRuleError(T(
                "TicketTargetStatusMissing",
                "Target ticket status is not configured.")));
        }

        var updated = await _uow.Tickets.UpdateStatusAsync(
            new TicketStatusUpdateDalDto
            {
                Id = ticket.Id,
                ManagementCompanyId = managementCompanyId,
                TicketStatusId = targetStatus.Id,
                ClosedAt = targetStatusCode == TicketWorkflowConstants.Closed ? DateTime.UtcNow : null
            },
            cancellationToken);

        return updated
            ? Result.Ok()
            : Result.Fail(new NotFoundError(T("TicketNotFound", "Ticket was not found.")));
    }

    private async Task<TicketOptionModel?> FindWorkStatusByCodeAsync(
        string code,
        CancellationToken cancellationToken)
    {
        var status = await _uow.Lookups.FindWorkStatusByCodeAsync(code, cancellationToken);
        return status is null ? null : MapOption(status);
    }

    private static ScheduledWorkListItemModel MapScheduledWorkListItem(ScheduledWorkListItemDalDto work)
    {
        return new ScheduledWorkListItemModel
        {
            ScheduledWorkId = work.Id,
            VendorId = work.VendorId,
            VendorName = work.VendorName,
            WorkStatusId = work.WorkStatusId,
            WorkStatusCode = work.WorkStatusCode,
            WorkStatusLabel = work.WorkStatusLabel,
            ScheduledStart = work.ScheduledStart,
            ScheduledEnd = work.ScheduledEnd,
            RealStart = work.RealStart,
            RealEnd = work.RealEnd,
            Notes = work.Notes,
            CreatedAt = work.CreatedAt,
            WorkLogCount = work.WorkLogCount
        };
    }

    private static ScheduledWorkDetailsModel MapScheduledWorkDetails(ScheduledWorkDetailsDalDto work)
    {
        return new ScheduledWorkDetailsModel
        {
            CompanySlug = work.CompanySlug,
            CompanyName = work.CompanyName,
            TicketId = work.TicketId,
            TicketNr = work.TicketNr,
            TicketTitle = work.TicketTitle,
            ScheduledWorkId = work.Id,
            VendorId = work.VendorId,
            VendorName = work.VendorName,
            WorkStatusId = work.WorkStatusId,
            WorkStatusCode = work.WorkStatusCode,
            WorkStatusLabel = work.WorkStatusLabel,
            ScheduledStart = work.ScheduledStart,
            ScheduledEnd = work.ScheduledEnd,
            RealStart = work.RealStart,
            RealEnd = work.RealEnd,
            Notes = work.Notes,
            CreatedAt = work.CreatedAt,
            WorkLogCount = work.WorkLogCount
        };
    }

    private static TicketOptionModel MapOption(App.DAL.DTO.Tickets.TicketOptionDalDto option)
    {
        return new TicketOptionModel
        {
            Id = option.Id,
            Code = option.Code,
            Label = option.Label
        };
    }

    private static ScheduledWorkBllDto NormalizeScheduledWork(ScheduledWorkBllDto dto)
    {
        return new ScheduledWorkBllDto
        {
            Id = dto.Id,
            VendorId = dto.VendorId,
            TicketId = dto.TicketId,
            WorkStatusId = dto.WorkStatusId,
            ScheduledStart = dto.ScheduledStart,
            ScheduledEnd = dto.ScheduledEnd,
            RealStart = dto.RealStart,
            RealEnd = dto.RealEnd,
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim()
        };
    }

    private static Result ValidateTicketStatusForWorkAction(
        string statusCode,
        string minimumStatusCode,
        string blockedAtOrAfterStatusCode,
        string errorMessage)
    {
        var currentIndex = StatusIndex(statusCode);
        var minimumIndex = StatusIndex(minimumStatusCode);
        var blockedIndex = StatusIndex(blockedAtOrAfterStatusCode);

        return currentIndex >= minimumIndex && currentIndex < blockedIndex
            ? Result.Ok()
            : Result.Fail(new BusinessRuleError(errorMessage));
    }

    private static int StatusIndex(string statusCode)
    {
        for (var i = 0; i < TicketWorkflowConstants.StatusOrder.Count; i++)
        {
            if (string.Equals(TicketWorkflowConstants.StatusOrder[i], statusCode, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static ValidationFailureModel Failure(string propertyName, string errorMessage)
    {
        return new ValidationFailureModel
        {
            PropertyName = propertyName,
            ErrorMessage = errorMessage
        };
    }

    private static string T(string key, string fallback)
    {
        return App.Resources.Views.UiText.ResourceManager.GetString(key) ?? fallback;
    }

    private static class TicketWorkflowConstants
    {
        public const string Assigned = "ASSIGNED";
        public const string Scheduled = "SCHEDULED";
        public const string InProgress = "IN_PROGRESS";
        public const string Completed = "COMPLETED";
        public const string Closed = "CLOSED";

        public static readonly IReadOnlyList<string> StatusOrder =
        [
            "CREATED",
            Assigned,
            Scheduled,
            InProgress,
            Completed,
            Closed
        ];
    }

    private static class WorkWorkflowConstants
    {
        public const string Scheduled = "SCHEDULED";
        public const string InProgress = "IN_PROGRESS";
        public const string Done = "DONE";
        public const string Cancelled = "CANCELLED";
    }

    private sealed record ScheduledWorkTicketContext(
        CompanyWorkspaceModel Workspace,
        TicketDetailsDalDto Ticket);

    private sealed record ScheduledWorkContext(
        CompanyWorkspaceModel Workspace,
        TicketDetailsDalDto Ticket,
        ScheduledWorkDalDto ScheduledWork);
}
