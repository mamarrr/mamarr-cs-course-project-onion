using App.BLL.Contracts.Tickets;
using App.BLL.DTO.Common;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Customers.Models;
using App.BLL.DTO.ScheduledWorks;
using App.BLL.DTO.Tickets.Models;
using App.BLL.DTO.WorkLogs;
using App.BLL.DTO.WorkLogs.Models;
using App.BLL.Mappers.WorkLogs;
using App.DAL.Contracts;
using App.DAL.Contracts.Repositories;
using App.DAL.DTO.ScheduledWorks;
using App.DAL.DTO.Tickets;
using App.DAL.DTO.WorkLogs;
using Base.BLL;
using FluentResults;

namespace App.BLL.Services.Tickets;

public class WorkLogService :
    BaseService<WorkLogBllDto, WorkLogDalDto, IWorkLogRepository, IAppUOW>,
    IWorkLogService
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

    private static readonly HashSet<string> CostVisibleRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "OWNER",
        "MANAGER",
        "FINANCE"
    };

    private readonly IAppUOW _uow;
    private readonly WorkLogBllDtoMapper _workLogMapper = new();

    public WorkLogService(IAppUOW uow)
        : base(uow.WorkLogs, uow, new WorkLogBllDtoMapper())
    {
        _uow = uow;
    }

    public async Task<Result<WorkLogListModel>> ListForScheduledWorkAsync(
        ScheduledWorkRoute route,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveScheduledWorkContextAsync(route, ReadAllowedRoleCodes, cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail<WorkLogListModel>(context.Errors);
        }

        var details = await _uow.ScheduledWorks.FindDetailsAsync(
            route.ScheduledWorkId,
            context.Value.Workspace.ManagementCompanyId,
            cancellationToken);
        if (details is null || details.TicketId != route.TicketId)
        {
            return Result.Fail<WorkLogListModel>(new NotFoundError(T("ScheduledWorkNotFound", "Scheduled work was not found.")));
        }

        var items = await _uow.WorkLogs.AllByScheduledWorkAsync(
            route.ScheduledWorkId,
            context.Value.Workspace.ManagementCompanyId,
            cancellationToken);
        var totals = await _uow.WorkLogs.TotalsForScheduledWorkAsync(
            route.ScheduledWorkId,
            context.Value.Workspace.ManagementCompanyId,
            cancellationToken);
        var canViewCosts = CostVisibleRoleCodes.Contains(context.Value.Workspace.RoleCode ?? string.Empty);

        return Result.Ok(new WorkLogListModel
        {
            CompanySlug = context.Value.Workspace.CompanySlug,
            CompanyName = context.Value.Workspace.CompanyName,
            TicketId = details.TicketId,
            TicketNr = details.TicketNr,
            TicketTitle = details.TicketTitle,
            ScheduledWorkId = details.Id,
            VendorName = details.VendorName,
            WorkStatusLabel = details.WorkStatusLabel,
            CanViewCosts = canViewCosts,
            Totals = MapWorkLogTotals(totals, canViewCosts),
            Items = items.Select(item => MapWorkLogListItem(item, canViewCosts)).ToList()
        });
    }

    public async Task<Result<WorkLogFormModel>> GetCreateFormAsync(
        ScheduledWorkRoute route,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveScheduledWorkContextAsync(route, WriteAllowedRoleCodes, cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail<WorkLogFormModel>(context.Errors);
        }

        var closedGuard = ValidateTicketNotClosedForWorkLogMutation(context.Value.Ticket);
        if (closedGuard.IsFailed)
        {
            return Result.Fail<WorkLogFormModel>(closedGuard.Errors);
        }

        return await BuildWorkLogFormAsync(context.Value, null, cancellationToken);
    }

    public async Task<Result<WorkLogFormModel>> GetEditFormAsync(
        WorkLogRoute route,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveWorkLogContextAsync(route, WriteAllowedRoleCodes, cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail<WorkLogFormModel>(context.Errors);
        }

        var closedGuard = ValidateTicketNotClosedForWorkLogMutation(context.Value.ScheduledContext.Ticket);
        if (closedGuard.IsFailed)
        {
            return Result.Fail<WorkLogFormModel>(closedGuard.Errors);
        }

        return await BuildWorkLogFormAsync(context.Value.ScheduledContext, context.Value.WorkLog, cancellationToken);
    }

    public async Task<Result<WorkLogDeleteModel>> GetDeleteModelAsync(
        WorkLogRoute route,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveWorkLogContextAsync(route, DeleteAllowedRoleCodes, cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail<WorkLogDeleteModel>(context.Errors);
        }

        var closedGuard = ValidateTicketNotClosedForWorkLogMutation(context.Value.ScheduledContext.Ticket);
        if (closedGuard.IsFailed)
        {
            return Result.Fail<WorkLogDeleteModel>(closedGuard.Errors);
        }

        var details = await _uow.ScheduledWorks.FindDetailsAsync(
            route.ScheduledWorkId,
            context.Value.ScheduledContext.Workspace.ManagementCompanyId,
            cancellationToken);
        if (details is null)
        {
            return Result.Fail<WorkLogDeleteModel>(new NotFoundError(T("ScheduledWorkNotFound", "Scheduled work was not found.")));
        }

        return Result.Ok(new WorkLogDeleteModel
        {
            CompanySlug = context.Value.ScheduledContext.Workspace.CompanySlug,
            CompanyName = context.Value.ScheduledContext.Workspace.CompanyName,
            TicketId = details.TicketId,
            TicketNr = details.TicketNr,
            ScheduledWorkId = details.Id,
            WorkLogId = context.Value.WorkLog.Id,
            VendorName = details.VendorName,
            Description = context.Value.WorkLog.Description
        });
    }

    public async Task<Result<WorkLogBllDto>> AddAsync(
        ScheduledWorkRoute route,
        WorkLogBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveScheduledWorkContextAsync(route, WriteAllowedRoleCodes, cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail<WorkLogBllDto>(context.Errors);
        }

        var closedGuard = ValidateTicketNotClosedForWorkLogMutation(context.Value.Ticket);
        if (closedGuard.IsFailed)
        {
            return Result.Fail<WorkLogBllDto>(closedGuard.Errors);
        }

        var validation = ValidateWorkLog(dto);
        if (validation.IsFailed)
        {
            return Result.Fail<WorkLogBllDto>(validation.Errors);
        }

        var normalized = NormalizeWorkLog(dto);
        normalized.Id = Guid.Empty;
        normalized.ScheduledWorkId = route.ScheduledWorkId;
        normalized.AppUserId = route.AppUserId;
        if (!CostVisibleRoleCodes.Contains(context.Value.Workspace.RoleCode ?? string.Empty))
        {
            normalized.MaterialCost = null;
            normalized.LaborCost = null;
        }

        var dalDto = _workLogMapper.Map(normalized);
        if (dalDto is null)
        {
            return Result.Fail<WorkLogBllDto>("Work log mapping failed.");
        }

        var id = _uow.WorkLogs.Add(dalDto);
        await _uow.SaveChangesAsync(cancellationToken);

        var created = await _uow.WorkLogs.FindInCompanyAsync(
            id,
            context.Value.Workspace.ManagementCompanyId,
            cancellationToken);

        return created is null
            ? Result.Fail<WorkLogBllDto>(new NotFoundError(T("WorkLogNotFound", "Work log was not found.")))
            : Result.Ok(_workLogMapper.Map(created)!);
    }

    public async Task<Result<WorkLogBllDto>> UpdateAsync(
        WorkLogRoute route,
        WorkLogBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveWorkLogContextAsync(route, WriteAllowedRoleCodes, cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail<WorkLogBllDto>(context.Errors);
        }

        var closedGuard = ValidateTicketNotClosedForWorkLogMutation(context.Value.ScheduledContext.Ticket);
        if (closedGuard.IsFailed)
        {
            return Result.Fail<WorkLogBllDto>(closedGuard.Errors);
        }

        var validation = ValidateWorkLog(dto);
        if (validation.IsFailed)
        {
            return Result.Fail<WorkLogBllDto>(validation.Errors);
        }

        var normalized = NormalizeWorkLog(dto);
        normalized.Id = route.WorkLogId;
        normalized.ScheduledWorkId = route.ScheduledWorkId;
        normalized.AppUserId = context.Value.WorkLog.AppUserId;
        if (!CostVisibleRoleCodes.Contains(context.Value.ScheduledContext.Workspace.RoleCode ?? string.Empty))
        {
            normalized.MaterialCost = context.Value.WorkLog.MaterialCost;
            normalized.LaborCost = context.Value.WorkLog.LaborCost;
        }

        var dalDto = _workLogMapper.Map(normalized);
        if (dalDto is null)
        {
            return Result.Fail<WorkLogBllDto>("Work log mapping failed.");
        }

        var updated = await _uow.WorkLogs.UpdateAsync(
            dalDto,
            context.Value.ScheduledContext.Workspace.ManagementCompanyId,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Ok(_workLogMapper.Map(updated)!);
    }

    public async Task<Result> DeleteAsync(
        WorkLogRoute route,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveWorkLogContextAsync(route, DeleteAllowedRoleCodes, cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail(context.Errors);
        }

        var closedGuard = ValidateTicketNotClosedForWorkLogMutation(context.Value.ScheduledContext.Ticket);
        if (closedGuard.IsFailed)
        {
            return Result.Fail(closedGuard.Errors);
        }

        var deleted = await _uow.WorkLogs.DeleteInCompanyAsync(
            route.WorkLogId,
            context.Value.ScheduledContext.Workspace.ManagementCompanyId,
            cancellationToken);
        if (!deleted)
        {
            return Result.Fail(new NotFoundError(T("WorkLogNotFound", "Work log was not found.")));
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

    private async Task<Result<WorkLogContext>> ResolveWorkLogContextAsync(
        WorkLogRoute route,
        ISet<string> allowedRoleCodes,
        CancellationToken cancellationToken)
    {
        var scheduledContext = await ResolveScheduledWorkContextAsync(route, allowedRoleCodes, cancellationToken);
        if (scheduledContext.IsFailed)
        {
            return Result.Fail<WorkLogContext>(scheduledContext.Errors);
        }

        var workLog = await _uow.WorkLogs.FindInCompanyAsync(
            route.WorkLogId,
            scheduledContext.Value.Workspace.ManagementCompanyId,
            cancellationToken);
        if (workLog is null || workLog.ScheduledWorkId != route.ScheduledWorkId)
        {
            return Result.Fail<WorkLogContext>(new NotFoundError(T("WorkLogNotFound", "Work log was not found.")));
        }

        return Result.Ok(new WorkLogContext(scheduledContext.Value, workLog));
    }

    private async Task<Result<WorkLogFormModel>> BuildWorkLogFormAsync(
        ScheduledWorkContext context,
        WorkLogDalDto? workLog,
        CancellationToken cancellationToken)
    {
        var details = await _uow.ScheduledWorks.FindDetailsAsync(
            context.ScheduledWork.Id,
            context.Workspace.ManagementCompanyId,
            cancellationToken);
        if (details is null || details.TicketId != context.Ticket.Id)
        {
            return Result.Fail<WorkLogFormModel>(new NotFoundError(T("ScheduledWorkNotFound", "Scheduled work was not found.")));
        }

        return Result.Ok(new WorkLogFormModel
        {
            CompanySlug = context.Workspace.CompanySlug,
            CompanyName = context.Workspace.CompanyName,
            TicketId = context.Ticket.Id,
            TicketNr = context.Ticket.TicketNr,
            TicketTitle = context.Ticket.Title,
            ScheduledWorkId = context.ScheduledWork.Id,
            WorkLogId = workLog?.Id,
            VendorName = details.VendorName,
            CanViewCosts = CostVisibleRoleCodes.Contains(context.Workspace.RoleCode ?? string.Empty),
            WorkStart = workLog?.WorkStart,
            WorkEnd = workLog?.WorkEnd,
            Hours = workLog?.Hours,
            MaterialCost = workLog?.MaterialCost,
            LaborCost = workLog?.LaborCost,
            Description = workLog?.Description
        });
    }

    private static Result ValidateWorkLog(WorkLogBllDto dto)
    {
        var failures = new List<ValidationFailureModel>();

        if (dto.Hours is < 0)
        {
            failures.Add(Failure(nameof(dto.Hours), T("WorkLogHoursNonNegative", "Hours must be zero or greater.")));
        }

        if (dto.MaterialCost is < 0)
        {
            failures.Add(Failure(nameof(dto.MaterialCost), T("WorkLogMaterialCostNonNegative", "Material cost must be zero or greater.")));
        }

        if (dto.LaborCost is < 0)
        {
            failures.Add(Failure(nameof(dto.LaborCost), T("WorkLogLaborCostNonNegative", "Labor cost must be zero or greater.")));
        }

        if (dto.WorkEnd.HasValue && dto.WorkStart.HasValue && dto.WorkEnd.Value < dto.WorkStart.Value)
        {
            failures.Add(Failure(nameof(dto.WorkEnd), T("WorkLogEndCannotBeBeforeStart", "Work end cannot be before work start.")));
        }

        if (!string.IsNullOrWhiteSpace(dto.Description) && dto.Description.Trim().Length > 4000)
        {
            failures.Add(Failure(nameof(dto.Description), T("WorkLogDescriptionMaxLength", "Description must be 4000 characters or fewer.")));
        }

        var hasMeaningfulField = dto.WorkStart.HasValue
                                 || dto.WorkEnd.HasValue
                                 || dto.Hours.HasValue
                                 || dto.MaterialCost.HasValue
                                 || dto.LaborCost.HasValue
                                 || !string.IsNullOrWhiteSpace(dto.Description);
        if (!hasMeaningfulField)
        {
            failures.Add(Failure(nameof(dto.Description), T("WorkLogRequiresMeaningfulField", "Enter at least one work log value.")));
        }

        return failures.Count == 0
            ? Result.Ok()
            : Result.Fail(new ValidationAppError("Validation failed.", failures));
    }

    private static Result ValidateTicketNotClosedForWorkLogMutation(TicketDetailsDalDto ticket)
    {
        return string.Equals(ticket.StatusCode, TicketWorkflowConstants.Closed, StringComparison.OrdinalIgnoreCase)
            ? Result.Fail(new BusinessRuleError(T(
                "WorkLogClosedTicketBlocked",
                "Work logs cannot be changed after the ticket is closed.")))
            : Result.Ok();
    }

    private static WorkLogBllDto NormalizeWorkLog(WorkLogBllDto dto)
    {
        return new WorkLogBllDto
        {
            Id = dto.Id,
            ScheduledWorkId = dto.ScheduledWorkId,
            AppUserId = dto.AppUserId,
            WorkStart = dto.WorkStart,
            WorkEnd = dto.WorkEnd,
            Hours = dto.Hours,
            MaterialCost = dto.MaterialCost,
            LaborCost = dto.LaborCost,
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim()
        };
    }

    private static WorkLogListItemModel MapWorkLogListItem(WorkLogListItemDalDto log, bool canViewCosts)
    {
        return new WorkLogListItemModel
        {
            WorkLogId = log.Id,
            AppUserId = log.AppUserId,
            AppUserName = string.IsNullOrWhiteSpace(log.AppUserName) ? "-" : log.AppUserName,
            WorkStart = log.WorkStart,
            WorkEnd = log.WorkEnd,
            Hours = log.Hours,
            MaterialCost = canViewCosts ? log.MaterialCost : null,
            LaborCost = canViewCosts ? log.LaborCost : null,
            Description = log.Description,
            CreatedAt = log.CreatedAt
        };
    }

    private static WorkLogTotalsModel MapWorkLogTotals(WorkLogTotalsDalDto totals, bool canViewCosts)
    {
        return new WorkLogTotalsModel
        {
            Count = totals.Count,
            Hours = totals.Hours,
            MaterialCost = canViewCosts ? totals.MaterialCost : 0m,
            LaborCost = canViewCosts ? totals.LaborCost : 0m,
            TotalCost = canViewCosts ? totals.TotalCost : 0m
        };
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
        public const string Closed = "CLOSED";
    }

    private sealed record ScheduledWorkTicketContext(
        CompanyWorkspaceModel Workspace,
        TicketDetailsDalDto Ticket);

    private sealed record ScheduledWorkContext(
        CompanyWorkspaceModel Workspace,
        TicketDetailsDalDto Ticket,
        ScheduledWorkDalDto ScheduledWork);

    private sealed record WorkLogContext(
        ScheduledWorkContext ScheduledContext,
        WorkLogDalDto WorkLog);
}
