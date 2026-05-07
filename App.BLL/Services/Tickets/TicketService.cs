using App.BLL.Contracts.Common;
using App.BLL.Contracts.Common.Deletion;
using App.BLL.Contracts.Customers;
using App.BLL.Contracts.Tickets;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Common;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Customers.Models;
using App.BLL.DTO.ScheduledWorks;
using App.BLL.DTO.ScheduledWorks.Models;
using App.BLL.DTO.Tickets;
using App.BLL.DTO.Tickets.Models;
using App.BLL.Mappers.ScheduledWorks;
using App.BLL.Mappers.Tickets;
using App.DAL.Contracts;
using App.DAL.Contracts.Repositories;
using App.DAL.DTO.ScheduledWorks;
using App.DAL.DTO.Tickets;
using Base.BLL;
using FluentResults;

namespace App.BLL.Services.Tickets;

public class TicketService :
    BaseService<TicketBllDto, TicketDalDto, ITicketRepository, IAppUOW>,
    ITicketService
{
    private const int TicketNrMaxLength = 20;

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

    private readonly ICustomerService _customerService;
    private readonly IAppUOW _uow;
    private readonly IAppDeleteGuard _deleteGuard;
    private readonly ScheduledWorkBllDtoMapper _scheduledWorkMapper = new();

    public TicketService(
        ICustomerService customerService,
        IAppUOW uow,
        IAppDeleteGuard deleteGuard)
        : base(uow.Tickets, uow, new TicketBllDtoMapper())
    {
        _customerService = customerService;
        _uow = uow;
        _deleteGuard = deleteGuard;
    }

    public async Task<Result<ManagementTicketsModel>> SearchAsync(
        ManagementTicketSearchRoute route,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveCompanyWorkspaceAsync(route.AppUserId, route.CompanySlug, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        var filter = ToFilterDto(route);
        var tickets = await _uow.Tickets.AllByCompanyAsync(
            workspace.Value.ManagementCompanyId,
            filter,
            cancellationToken);

        return Result.Ok(new ManagementTicketsModel
        {
            CompanySlug = workspace.Value.CompanySlug,
            CompanyName = workspace.Value.CompanyName,
            Tickets = tickets.Select(MapListItem).ToList(),
            Filter = new TicketFilterModel
            {
                Search = route.Search,
                StatusId = route.StatusId,
                PriorityId = route.PriorityId,
                CategoryId = route.CategoryId,
                CustomerId = route.CustomerId,
                PropertyId = route.PropertyId,
                UnitId = route.UnitId,
                VendorId = route.VendorId,
                DueFrom = route.DueFrom,
                DueTo = route.DueTo
            },
            Options = await BuildOptionsAsync(
                workspace.Value.ManagementCompanyId,
                route.CustomerId,
                route.PropertyId,
                route.UnitId,
                route.CategoryId,
                cancellationToken)
        });
    }

    public async Task<Result<ManagementTicketDetailsModel>> GetDetailsAsync(
        TicketRoute route,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveCompanyWorkspaceAsync(route.AppUserId, route.CompanySlug, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        var ticket = await _uow.Tickets.FindDetailsAsync(
            route.TicketId,
            workspace.Value.ManagementCompanyId,
            cancellationToken);

        if (ticket is null)
        {
            return Result.Fail(new NotFoundError(T("TicketNotFound", "Ticket was not found.")));
        }

        var nextStatusCode = GetNextStatusCode(ticket.StatusCode);
        var statuses = nextStatusCode is null
            ? Array.Empty<TicketOptionModel>()
            : await GetStatusesAsync(cancellationToken);
        var nextStatus = statuses.FirstOrDefault(status => status.Code == nextStatusCode);
        var scheduledWork = await _uow.ScheduledWorks.AllByTicketAsync(
            route.TicketId,
            workspace.Value.ManagementCompanyId,
            cancellationToken);

        return Result.Ok(new ManagementTicketDetailsModel
        {
            CompanySlug = workspace.Value.CompanySlug,
            CompanyName = workspace.Value.CompanyName,
            TicketId = ticket.Id,
            TicketNr = ticket.TicketNr,
            Title = ticket.Title,
            Description = ticket.Description,
            StatusCode = ticket.StatusCode,
            StatusLabel = ticket.StatusLabel,
            PriorityLabel = ticket.PriorityLabel,
            CategoryLabel = ticket.CategoryLabel,
            CustomerName = ticket.CustomerName,
            CustomerSlug = ticket.CustomerSlug,
            PropertyName = ticket.PropertyName,
            PropertySlug = ticket.PropertySlug,
            UnitNr = ticket.UnitNr,
            UnitSlug = ticket.UnitSlug,
            ResidentName = ticket.ResidentName,
            ResidentIdCode = ticket.ResidentIdCode,
            VendorName = ticket.VendorName,
            CreatedAt = ticket.CreatedAt,
            DueAt = ticket.DueAt,
            ClosedAt = ticket.ClosedAt,
            NextStatusCode = nextStatus?.Code,
            NextStatusLabel = nextStatus?.Label,
            ScheduledWork = scheduledWork.Select(MapScheduledWorkListItem).ToList()
        });
    }

    public async Task<Result<ManagementTicketFormModel>> GetCreateFormAsync(
        TicketSelectorOptionsRoute route,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveCompanyWorkspaceAsync(route.AppUserId, route.CompanySlug, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        var options = await BuildOptionsAsync(
            workspace.Value.ManagementCompanyId,
            route.CustomerId,
            route.PropertyId,
            route.UnitId,
            route.CategoryId,
            cancellationToken);

        var createdStatus = options.Statuses.FirstOrDefault(status => status.Code == TicketWorkflowConstants.Created);
        var mediumPriority = options.Priorities.FirstOrDefault(priority => priority.Code == "MEDIUM");

        return Result.Ok(new ManagementTicketFormModel
        {
            CompanySlug = workspace.Value.CompanySlug,
            CompanyName = workspace.Value.CompanyName,
            TicketNr = await _uow.Tickets.GetNextTicketNrAsync(
                workspace.Value.ManagementCompanyId,
                DateTime.UtcNow,
                cancellationToken),
            TicketStatusId = createdStatus?.Id ?? Guid.Empty,
            TicketPriorityId = mediumPriority?.Id ?? Guid.Empty,
            CustomerId = route.CustomerId,
            PropertyId = route.PropertyId,
            UnitId = route.UnitId,
            Options = options
        });
    }

    public async Task<Result<ManagementTicketFormModel>> GetEditFormAsync(
        TicketRoute route,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveCompanyWorkspaceAsync(route.AppUserId, route.CompanySlug, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        var ticket = await _uow.Tickets.FindForEditAsync(
            route.TicketId,
            workspace.Value.ManagementCompanyId,
            cancellationToken);

        if (ticket is null)
        {
            return Result.Fail(new NotFoundError(T("TicketNotFound", "Ticket was not found.")));
        }

        return Result.Ok(new ManagementTicketFormModel
        {
            CompanySlug = workspace.Value.CompanySlug,
            CompanyName = workspace.Value.CompanyName,
            TicketId = ticket.Id,
            TicketNr = ticket.TicketNr,
            Title = ticket.Title.ToString(),
            Description = ticket.Description.ToString(),
            TicketCategoryId = ticket.TicketCategoryId,
            TicketStatusId = ticket.TicketStatusId,
            TicketPriorityId = ticket.TicketPriorityId,
            CustomerId = ticket.CustomerId,
            PropertyId = ticket.PropertyId,
            UnitId = ticket.UnitId,
            ResidentId = ticket.ResidentId,
            VendorId = ticket.VendorId,
            DueAt = ticket.DueAt,
            Options = await BuildOptionsAsync(
                workspace.Value.ManagementCompanyId,
                ticket.CustomerId,
                ticket.PropertyId,
                ticket.UnitId,
                ticket.TicketCategoryId,
                cancellationToken)
        });
    }

    public async Task<Result<TicketSelectorOptionsModel>> GetSelectorOptionsAsync(
        TicketSelectorOptionsRoute route,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveCompanyWorkspaceAsync(route.AppUserId, route.CompanySlug, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        return Result.Ok(await BuildOptionsAsync(
            workspace.Value.ManagementCompanyId,
            route.CustomerId,
            route.PropertyId,
            route.UnitId,
            route.CategoryId,
            cancellationToken));
    }

    public async Task<Result<TicketBllDto>> CreateAsync(
        ManagementCompanyRoute route,
        TicketBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateCreate(dto);
        if (validation.IsFailed)
        {
            return Result.Fail<TicketBllDto>(validation.Errors);
        }

        var workspace = await ResolveCompanyWorkspaceAsync(route.AppUserId, route.CompanySlug, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail<TicketBllDto>(workspace.Errors);
        }

        var createdStatus = await _uow.Lookups.FindTicketStatusByCodeAsync(
            TicketWorkflowConstants.Created,
            cancellationToken);
        if (createdStatus is null)
        {
            return Result.Fail(new BusinessRuleError(T("TicketCreatedStatusMissing", "Created ticket status is not configured.")));
        }

        var referenceValidation = await ValidateReferencesAsync(
            workspace.Value.ManagementCompanyId,
            dto.TicketCategoryId,
            dto.TicketPriorityId,
            createdStatus.Id,
            dto.CustomerId,
            dto.PropertyId,
            dto.UnitId,
            dto.ResidentId,
            dto.VendorId,
            requireCascadingParents: true,
            cancellationToken);

        if (referenceValidation.IsFailed)
        {
            return Result.Fail<TicketBllDto>(referenceValidation.Errors);
        }

        var normalized = Normalize(dto);
        var duplicate = await _uow.Tickets.TicketNrExistsAsync(
            workspace.Value.ManagementCompanyId,
            normalized.TicketNr,
            cancellationToken: cancellationToken);

        if (duplicate)
        {
            return Result.Fail(new ConflictError(T("TicketNumberAlreadyExists", "Ticket number already exists in this company.")));
        }

        dto.Id = Guid.Empty;
        dto.ManagementCompanyId = workspace.Value.ManagementCompanyId;
        dto.TicketStatusId = createdStatus.Id;
        dto.TicketNr = normalized.TicketNr;
        dto.Title = normalized.Title;
        dto.Description = normalized.Description;
        dto.ClosedAt = null;

        return await AddAndFindCoreAsync(dto, workspace.Value.ManagementCompanyId, cancellationToken);
    }

    public async Task<Result<TicketBllDto>> UpdateAsync(
        TicketRoute route,
        TicketBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateUpdate(dto);
        if (validation.IsFailed)
        {
            return Result.Fail<TicketBllDto>(validation.Errors);
        }

        var workspace = await ResolveCompanyWorkspaceAsync(route.AppUserId, route.CompanySlug, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail<TicketBllDto>(workspace.Errors);
        }

        var existing = await _uow.Tickets.FindForEditAsync(
            route.TicketId,
            workspace.Value.ManagementCompanyId,
            cancellationToken);
        if (existing is null)
        {
            return Result.Fail(new NotFoundError(T("TicketNotFound", "Ticket was not found.")));
        }

        var targetStatus = await _uow.Lookups.FindTicketStatusByIdAsync(dto.TicketStatusId, cancellationToken);
        if (targetStatus is null)
        {
            return Result.Fail(new ValidationAppError("Validation failed.", [
                new ValidationFailureModel
                {
                    PropertyName = nameof(dto.TicketStatusId),
                    ErrorMessage = T("InvalidTicketStatus", "Selected ticket status is invalid.")
                }
            ]));
        }

        if (string.IsNullOrWhiteSpace(targetStatus.Code))
        {
            return Result.Fail(new BusinessRuleError(T("InvalidTicketStatus", "Selected ticket status is invalid.")));
        }

        var referenceValidation = await ValidateReferencesAsync(
            workspace.Value.ManagementCompanyId,
            dto.TicketCategoryId,
            dto.TicketPriorityId,
            dto.TicketStatusId,
            dto.CustomerId,
            dto.PropertyId,
            dto.UnitId,
            dto.ResidentId,
            dto.VendorId,
            requireCascadingParents: true,
            cancellationToken);

        if (referenceValidation.IsFailed)
        {
            return Result.Fail<TicketBllDto>(referenceValidation.Errors);
        }

        var statusGuard = ValidateStatusGuard(targetStatus.Code, dto.VendorId, dto.DueAt);
        if (statusGuard.IsFailed)
        {
            return Result.Fail<TicketBllDto>(statusGuard.Errors);
        }

        var normalized = Normalize(dto);
        var duplicate = await _uow.Tickets.TicketNrExistsAsync(
            workspace.Value.ManagementCompanyId,
            normalized.TicketNr,
            route.TicketId,
            cancellationToken);

        if (duplicate)
        {
            return Result.Fail(new ConflictError(T("TicketNumberAlreadyExists", "Ticket number already exists in this company.")));
        }

        dto.Id = route.TicketId;
        dto.ManagementCompanyId = workspace.Value.ManagementCompanyId;
        dto.TicketNr = normalized.TicketNr;
        dto.Title = normalized.Title;
        dto.Description = normalized.Description;
        dto.ClosedAt = targetStatus.Code == TicketWorkflowConstants.Closed ? DateTime.UtcNow : null;

        var updated = await base.UpdateAsync(dto, workspace.Value.ManagementCompanyId, cancellationToken);
        if (updated.IsFailed)
        {
            return Result.Fail<TicketBllDto>(updated.Errors);
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return updated;
    }

    public async Task<Result> DeleteAsync(
        TicketRoute route,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveCompanyWorkspaceAsync(route.AppUserId, route.CompanySlug, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        var ticket = await _uow.Tickets.FindDetailsAsync(
            route.TicketId,
            workspace.Value.ManagementCompanyId,
            cancellationToken);
        if (ticket is null)
        {
            return Result.Fail(new NotFoundError(T("TicketNotFound", "Ticket was not found.")));
        }

        var canDelete = await _deleteGuard.CanDeleteTicketAsync(
            route.TicketId,
            workspace.Value.ManagementCompanyId,
            cancellationToken);
        if (!canDelete)
        {
            return Result.Fail(new BusinessRuleError(DeleteBlockedMessage()));
        }

        var removed = await base.RemoveAsync(route.TicketId, workspace.Value.ManagementCompanyId, cancellationToken);
        if (removed.IsFailed)
        {
            return Result.Fail(removed.Errors);
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result<TicketBllDto>> AdvanceStatusAsync(
        TicketRoute route,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveCompanyWorkspaceAsync(route.AppUserId, route.CompanySlug, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail<TicketBllDto>(workspace.Errors);
        }

        var advanced = await AdvanceStatusCoreAsync(
            route.TicketId,
            workspace.Value.ManagementCompanyId,
            cancellationToken);
        if (advanced.IsFailed)
        {
            return Result.Fail<TicketBllDto>(advanced.Errors);
        }

        return await FindAsync(route.TicketId, workspace.Value.ManagementCompanyId, cancellationToken);
    }

    public async Task<Result<ScheduledWorkListModel>> ListScheduledWorkForTicketAsync(
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

    public async Task<Result<ScheduledWorkDetailsModel>> GetScheduledWorkDetailsAsync(
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

    public async Task<Result<ScheduledWorkFormModel>> GetScheduleCreateFormAsync(
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

    public async Task<Result<ScheduledWorkFormModel>> GetScheduleEditFormAsync(
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

    public async Task<Result<ScheduledWorkBllDto>> ScheduleWorkAsync(
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

    public async Task<Result> DeleteScheduledWorkAsync(
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

    private async Task<Result> AdvanceStatusCoreAsync(
        Guid ticketId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _uow.Tickets.FindDetailsAsync(
            ticketId,
            managementCompanyId,
            cancellationToken);

        if (ticket is null)
        {
            return Result.Fail(new NotFoundError(T("TicketNotFound", "Ticket was not found.")));
        }

        var nextStatusCode = GetNextStatusCode(ticket.StatusCode);
        if (nextStatusCode is null)
        {
            return Result.Fail(new BusinessRuleError(T("TicketAlreadyAtFinalStatus", "Ticket is already at the final status.")));
        }

        var scheduledWorkGuard = await ValidateScheduledWorkTransitionPrerequisiteAsync(
            ticket.Id,
            managementCompanyId,
            nextStatusCode,
            cancellationToken);
        if (scheduledWorkGuard.IsFailed)
        {
            return Result.Fail(scheduledWorkGuard.Errors);
        }

        var nextStatus = await _uow.Lookups.FindTicketStatusByCodeAsync(nextStatusCode, cancellationToken);
        if (nextStatus is null)
        {
            return Result.Fail(new BusinessRuleError(T("TicketNextStatusMissing", "Next ticket status is not configured.")));
        }

        var updated = await _uow.Tickets.UpdateStatusAsync(
            new TicketStatusUpdateDalDto
            {
                Id = ticketId,
                ManagementCompanyId = managementCompanyId,
                TicketStatusId = nextStatus.Id,
                ClosedAt = nextStatusCode == TicketWorkflowConstants.Closed ? DateTime.UtcNow : null
            },
            cancellationToken);

        if (!updated)
        {
            return Result.Fail(new NotFoundError(T("TicketNotFound", "Ticket was not found.")));
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    private async Task<Result<CompanyWorkspaceModel>> ResolveCompanyWorkspaceAsync(
        Guid userId,
        string companySlug,
        CancellationToken cancellationToken)
    {
        return await _customerService.ResolveCompanyWorkspaceAsync(
            new App.BLL.DTO.Common.Routes.ManagementCompanyRoute
            {
                AppUserId = userId,
                CompanySlug = companySlug
            },
            cancellationToken);
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
            CompanyName = company.Name
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

    private async Task<Result> ValidateScheduledWorkTransitionPrerequisiteAsync(
        Guid ticketId,
        Guid managementCompanyId,
        string nextStatusCode,
        CancellationToken cancellationToken)
    {
        if (string.Equals(nextStatusCode, TicketWorkflowConstants.Scheduled, StringComparison.OrdinalIgnoreCase)
            && !await _uow.ScheduledWorks.ExistsForTicketAsync(ticketId, managementCompanyId, cancellationToken))
        {
            return Result.Fail(new BusinessRuleError(T(
                "TicketScheduledRequiresScheduledWork",
                "Schedule vendor work before moving the ticket to scheduled.")));
        }

        if (string.Equals(nextStatusCode, TicketWorkflowConstants.InProgress, StringComparison.OrdinalIgnoreCase)
            && !await _uow.ScheduledWorks.AnyStartedForTicketAsync(ticketId, managementCompanyId, cancellationToken))
        {
            return Result.Fail(new BusinessRuleError(T(
                "TicketInProgressRequiresStartedWork",
                "Start scheduled work before moving the ticket to in progress.")));
        }

        if (string.Equals(nextStatusCode, TicketWorkflowConstants.Completed, StringComparison.OrdinalIgnoreCase)
            && !await _uow.ScheduledWorks.AnyCompletedForTicketAsync(ticketId, managementCompanyId, cancellationToken))
        {
            return Result.Fail(new BusinessRuleError(T(
                "TicketCompletedRequiresCompletedWork",
                "Complete scheduled work before moving the ticket to completed.")));
        }

        return Result.Ok();
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

    private async Task<TicketOptionModel?> FindWorkStatusByCodeAsync(
        string code,
        CancellationToken cancellationToken)
    {
        var status = await _uow.Lookups.FindWorkStatusByCodeAsync(code, cancellationToken);
        return status is null ? null : MapOption(status);
    }

    private async Task<TicketSelectorOptionsModel> BuildOptionsAsync(
        Guid managementCompanyId,
        Guid? customerId,
        Guid? propertyId,
        Guid? unitId,
        Guid? categoryId,
        CancellationToken cancellationToken)
    {
        return new TicketSelectorOptionsModel
        {
            Statuses = await GetStatusesAsync(cancellationToken),
            Priorities = (await _uow.Lookups.AllTicketPrioritiesAsync(cancellationToken)).Select(MapOption).ToList(),
            Categories = (await _uow.Lookups.AllTicketCategoriesAsync(cancellationToken)).Select(MapOption).ToList(),
            Customers = (await _uow.Customers.OptionsForTicketAsync(managementCompanyId, cancellationToken)).Select(MapOption).ToList(),
            Properties = (await _uow.Properties.OptionsForTicketAsync(managementCompanyId, customerId, cancellationToken)).Select(MapOption).ToList(),
            Units = (await _uow.Units.OptionsForTicketAsync(managementCompanyId, propertyId, cancellationToken)).Select(MapOption).ToList(),
            Residents = (await _uow.Residents.OptionsForTicketAsync(managementCompanyId, unitId, cancellationToken)).Select(MapOption).ToList(),
            Vendors = (await _uow.Vendors.OptionsForTicketAsync(managementCompanyId, categoryId, cancellationToken)).Select(MapOption).ToList()
        };
    }

    private async Task<IReadOnlyList<TicketOptionModel>> GetStatusesAsync(CancellationToken cancellationToken)
    {
        var statuses = (await _uow.Lookups.AllTicketStatusesAsync(cancellationToken))
            .Select(MapOption)
            .ToList();

        return statuses
            .OrderBy(status =>
            {
                var index = StatusIndex(status.Code ?? string.Empty);
                return index < 0 ? int.MaxValue : index;
            })
            .ToList();
    }

    private async Task<Result> ValidateReferencesAsync(
        Guid managementCompanyId,
        Guid categoryId,
        Guid priorityId,
        Guid statusId,
        Guid? customerId,
        Guid? propertyId,
        Guid? unitId,
        Guid? residentId,
        Guid? vendorId,
        bool requireCascadingParents,
        CancellationToken cancellationToken)
    {
        var categoryExists = await _uow.Lookups.TicketCategoryExistsAsync(categoryId, cancellationToken);
        var priorityExists = await _uow.Lookups.TicketPriorityExistsAsync(priorityId, cancellationToken);
        var statusExists = await _uow.Lookups.TicketStatusExistsAsync(statusId, cancellationToken);

        var customerBelongsToCompany = !customerId.HasValue ||
                                       await _uow.Customers.ExistsInCompanyAsync(customerId.Value, managementCompanyId, cancellationToken);

        var propertyBelongsToCompany = !propertyId.HasValue ||
                                       await _uow.Properties.ExistsInCompanyAsync(propertyId.Value, managementCompanyId, cancellationToken);

        var propertyBelongsToCustomer = !propertyId.HasValue || !customerId.HasValue ||
                                        await _uow.Properties.ExistsInCustomerAsync(propertyId.Value, customerId.Value, cancellationToken);

        var unitBelongsToCompany = !unitId.HasValue ||
                                   await _uow.Units.ExistsInCompanyAsync(unitId.Value, managementCompanyId, cancellationToken);

        var unitBelongsToProperty = !unitId.HasValue || !propertyId.HasValue ||
                                    await _uow.Units.ExistsInPropertyAsync(unitId.Value, propertyId.Value, cancellationToken);

        var residentBelongsToCompany = !residentId.HasValue ||
                                       await _uow.Residents.ExistsInCompanyAsync(residentId.Value, managementCompanyId, cancellationToken);

        var residentLinkedToUnit = !residentId.HasValue || !unitId.HasValue ||
                                   await _uow.Residents.IsLinkedToUnitAsync(residentId.Value, unitId.Value, cancellationToken);

        var vendorBelongsToCompany = !vendorId.HasValue ||
                                     await _uow.Vendors.ExistsInCompanyAsync(vendorId.Value, managementCompanyId, cancellationToken);

        var vendorSupportsCategory = !vendorId.HasValue ||
                                     await _uow.VendorTicketCategories.ExistsInCompanyAsync(
                                         vendorId.Value,
                                         categoryId,
                                         managementCompanyId,
                                         cancellationToken);

        var failures = new List<ValidationFailureModel>();

        if (!categoryExists)
        {
            failures.Add(Failure("TicketCategoryId", T("InvalidTicketCategory", "Selected ticket category is invalid.")));
        }

        if (!priorityExists)
        {
            failures.Add(Failure("TicketPriorityId", T("InvalidTicketPriority", "Selected ticket priority is invalid.")));
        }

        if (!statusExists)
        {
            failures.Add(Failure("TicketStatusId", T("InvalidTicketStatus", "Selected ticket status is invalid.")));
        }

        if (!customerBelongsToCompany)
        {
            failures.Add(Failure("CustomerId", T("InvalidTicketCustomer", "Selected customer is invalid.")));
        }

        if (requireCascadingParents && propertyId.HasValue && !customerId.HasValue)
        {
            failures.Add(Failure("CustomerId", T("TicketCustomerRequiredForProperty", "Select a customer before selecting a property.")));
        }

        if (!propertyBelongsToCompany || !propertyBelongsToCustomer)
        {
            failures.Add(Failure("PropertyId", T("InvalidTicketProperty", "Selected property is invalid for this customer.")));
        }

        if (requireCascadingParents && unitId.HasValue && !propertyId.HasValue)
        {
            failures.Add(Failure("PropertyId", T("TicketPropertyRequiredForUnit", "Select a property before selecting a unit.")));
        }

        if (!unitBelongsToCompany || !unitBelongsToProperty)
        {
            failures.Add(Failure("UnitId", T("InvalidTicketUnit", "Selected unit is invalid for this property.")));
        }

        if (!residentBelongsToCompany || !residentLinkedToUnit)
        {
            failures.Add(Failure("ResidentId", T("InvalidTicketResident", "Selected resident is invalid for this unit.")));
        }

        if (!vendorBelongsToCompany)
        {
            failures.Add(Failure("VendorId", T("InvalidTicketVendor", "Selected vendor is invalid.")));
        }
        else if (!vendorSupportsCategory)
        {
            failures.Add(Failure("VendorId", T(
                "TicketVendorDoesNotSupportCategory",
                "Selected vendor is not assigned to this ticket category.")));
        }

        return failures.Count == 0
            ? Result.Ok()
            : Result.Fail(new ValidationAppError("Validation failed.", failures));
    }

    private static Result ValidateCreate(TicketBllDto dto)
    {
        var failures = new List<ValidationFailureModel>();

        AddRequired(failures, nameof(dto.TicketNr), dto.TicketNr, T("TicketNumber", "Ticket number"));
        AddRequired(failures, nameof(dto.Title), dto.Title, T("Title", "Title"));
        AddRequired(failures, nameof(dto.Description), dto.Description, T("Description", "Description"));

        if (!string.IsNullOrWhiteSpace(dto.TicketNr) && dto.TicketNr.Trim().Length > TicketNrMaxLength)
        {
            failures.Add(Failure(nameof(dto.TicketNr), T("TicketNumberMaxLength", "Ticket number must be 20 characters or fewer.")));
        }

        if (dto.TicketCategoryId == Guid.Empty)
        {
            failures.Add(Failure(nameof(dto.TicketCategoryId), T("TicketCategoryRequired", "Ticket category is required.")));
        }

        if (dto.TicketPriorityId == Guid.Empty)
        {
            failures.Add(Failure(nameof(dto.TicketPriorityId), T("TicketPriorityRequired", "Ticket priority is required.")));
        }

        return failures.Count == 0
            ? Result.Ok()
            : Result.Fail(new ValidationAppError("Validation failed.", failures));
    }

    private static Result ValidateUpdate(TicketBllDto dto)
    {
        var failures = new List<ValidationFailureModel>();

        AddRequired(failures, nameof(dto.TicketNr), dto.TicketNr, T("TicketNumber", "Ticket number"));
        AddRequired(failures, nameof(dto.Title), dto.Title, T("Title", "Title"));
        AddRequired(failures, nameof(dto.Description), dto.Description, T("Description", "Description"));

        if (!string.IsNullOrWhiteSpace(dto.TicketNr) && dto.TicketNr.Trim().Length > TicketNrMaxLength)
        {
            failures.Add(Failure(nameof(dto.TicketNr), T("TicketNumberMaxLength", "Ticket number must be 20 characters or fewer.")));
        }

        if (dto.TicketCategoryId == Guid.Empty)
        {
            failures.Add(Failure(nameof(dto.TicketCategoryId), T("TicketCategoryRequired", "Ticket category is required.")));
        }

        if (dto.TicketStatusId == Guid.Empty)
        {
            failures.Add(Failure(nameof(dto.TicketStatusId), T("TicketStatusRequired", "Ticket status is required.")));
        }

        if (dto.TicketPriorityId == Guid.Empty)
        {
            failures.Add(Failure(nameof(dto.TicketPriorityId), T("TicketPriorityRequired", "Ticket priority is required.")));
        }

        return failures.Count == 0
            ? Result.Ok()
            : Result.Fail(new ValidationAppError("Validation failed.", failures));
    }

    private static Result ValidateStatusGuard(string statusCode, Guid? vendorId, DateTime? dueAt)
    {
        if (!IsAfterScheduled(statusCode) || vendorId.HasValue && dueAt.HasValue)
        {
            return Result.Ok();
        }

        return Result.Fail(new ValidationAppError("Validation failed.", [
            Failure("VendorId", T("TicketScheduledRequiresVendor", "Scheduled or later tickets require a vendor.")),
            Failure("DueAt", T("TicketScheduledRequiresDueDate", "Scheduled or later tickets require a due date."))
        ]));
    }

    private static TicketListFilterDalDto ToFilterDto(ManagementTicketSearchRoute route)
    {
        return new TicketListFilterDalDto
        {
            Search = string.IsNullOrWhiteSpace(route.Search) ? null : route.Search.Trim(),
            StatusId = route.StatusId,
            PriorityId = route.PriorityId,
            CategoryId = route.CategoryId,
            CustomerId = route.CustomerId,
            PropertyId = route.PropertyId,
            UnitId = route.UnitId,
            VendorId = route.VendorId,
            DueFrom = route.DueFrom,
            DueTo = route.DueTo
        };
    }

    private static ManagementTicketListItemModel MapListItem(TicketListItemDalDto ticket)
    {
        return new ManagementTicketListItemModel
        {
            TicketId = ticket.Id,
            TicketNr = ticket.TicketNr,
            Title = ticket.Title,
            StatusCode = ticket.StatusCode,
            StatusLabel = ticket.StatusLabel,
            PriorityLabel = ticket.PriorityLabel,
            CategoryLabel = ticket.CategoryLabel,
            CustomerName = ticket.CustomerName,
            CustomerSlug = ticket.CustomerSlug,
            PropertyName = ticket.PropertyName,
            PropertySlug = ticket.PropertySlug,
            UnitNr = ticket.UnitNr,
            UnitSlug = ticket.UnitSlug,
            ResidentName = ticket.ResidentName,
            ResidentIdCode = ticket.ResidentIdCode,
            VendorName = ticket.VendorName,
            DueAt = ticket.DueAt,
            CreatedAt = ticket.CreatedAt
        };
    }

    private static TicketOptionModel MapOption(TicketOptionDalDto option)
    {
        return new TicketOptionModel
        {
            Id = option.Id,
            Code = option.Code,
            Label = option.Label
        };
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

    private static string? GetNextStatusCode(string statusCode)
    {
        var index = StatusIndex(statusCode);
        return index < 0 || index >= TicketWorkflowConstants.StatusOrder.Count - 1
            ? null
            : TicketWorkflowConstants.StatusOrder[index + 1];
    }

    private static bool IsScheduledOrLater(string statusCode)
    {
        var index = StatusIndex(statusCode);
        var scheduledIndex = StatusIndex(TicketWorkflowConstants.Scheduled);
        return index >= scheduledIndex && scheduledIndex >= 0;
    }

    private static bool IsAfterScheduled(string statusCode)
    {
        var index = StatusIndex(statusCode);
        var scheduledIndex = StatusIndex(TicketWorkflowConstants.Scheduled);
        return index > scheduledIndex && scheduledIndex >= 0;
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

    private static void AddRequired(
        ICollection<ValidationFailureModel> failures,
        string propertyName,
        string? value,
        string label)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        failures.Add(Failure(
            propertyName,
            App.Resources.Views.UiText.RequiredField.Replace("{0}", label)));
    }

    private static ValidationFailureModel Failure(string propertyName, string errorMessage)
    {
        return new ValidationFailureModel
        {
            PropertyName = propertyName,
            ErrorMessage = errorMessage
        };
    }

    private static NormalizedTicket Normalize(TicketBllDto dto)
    {
        return new NormalizedTicket(
            dto.TicketNr.Trim(),
            dto.Title.Trim(),
            dto.Description.Trim());
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

    private static TicketRoute ToTicketRoute(ManagementCompanyRoute route, Guid ticketId)
    {
        return new TicketRoute
        {
            AppUserId = route.AppUserId,
            CompanySlug = route.CompanySlug,
            TicketId = ticketId
        };
    }

    private static string T(string key, string fallback)
    {
        return App.Resources.Views.UiText.ResourceManager.GetString(key) ?? fallback;
    }

    private static string DeleteBlockedMessage()
    {
        return T(
            "UnableToDeleteBecauseDependentRecordsExist",
            "Unable to delete because dependent records exist.");
    }

    private static class TicketWorkflowConstants
    {
        public const string Created = "CREATED";
        public const string Assigned = "ASSIGNED";
        public const string Scheduled = "SCHEDULED";
        public const string InProgress = "IN_PROGRESS";
        public const string Completed = "COMPLETED";
        public const string Closed = "CLOSED";

        public static readonly IReadOnlyList<string> StatusOrder =
        [
            Created,
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

    private sealed record NormalizedTicket(
        string TicketNr,
        string Title,
        string Description);

    private sealed record ScheduledWorkTicketContext(
        CompanyWorkspaceModel Workspace,
        TicketDetailsDalDto Ticket);

    private sealed record ScheduledWorkContext(
        CompanyWorkspaceModel Workspace,
        TicketDetailsDalDto Ticket,
        ScheduledWorkDalDto ScheduledWork);
}
