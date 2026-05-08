using App.BLL.Contracts.Common;
using App.BLL.Contracts.Tickets;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Common;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Customers.Models;
using App.BLL.DTO.ScheduledWorks.Models;
using App.BLL.DTO.Tickets;
using App.BLL.DTO.Tickets.Models;
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

    private static readonly HashSet<string> CostVisibleRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "OWNER",
        "MANAGER",
        "FINANCE"
    };

    private readonly IAppUOW _uow;

    public TicketService(IAppUOW uow)
        : base(uow.Tickets, uow, new TicketBllDtoMapper())
    {
        _uow = uow;
    }

    public async Task<Result<ManagementTicketsModel>> SearchAsync(
        ManagementTicketSearchRoute route,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveCompanyWorkspaceAsync(
            route.AppUserId,
            route.CompanySlug,
            WriteAllowedRoleCodes,
            cancellationToken);
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
        var workspace = await ResolveCompanyWorkspaceAsync(
            route.AppUserId,
            route.CompanySlug,
            ReadAllowedRoleCodes,
            cancellationToken);
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

        var availability = await BuildTransitionAvailabilityAsync(
            ticket,
            workspace.Value.ManagementCompanyId,
            cancellationToken);
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
            NextStatusCode = availability.NextStatusCode,
            NextStatusLabel = availability.NextStatusLabel,
            CanAdvanceStatus = availability.CanAdvance,
            TransitionBlockingReasons = availability.BlockingReasons,
            ScheduledWork = scheduledWork.Select(MapScheduledWorkListItem).ToList()
        });
    }

    public async Task<Result<ManagementTicketFormModel>> GetCreateFormAsync(
        TicketSelectorOptionsRoute route,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveCompanyWorkspaceAsync(
            route.AppUserId,
            route.CompanySlug,
            WriteAllowedRoleCodes,
            cancellationToken);
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
        var workspace = await ResolveCompanyWorkspaceAsync(
            route.AppUserId,
            route.CompanySlug,
            WriteAllowedRoleCodes,
            cancellationToken);
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
        var workspace = await ResolveCompanyWorkspaceAsync(
            route.AppUserId,
            route.CompanySlug,
            WriteAllowedRoleCodes,
            cancellationToken);
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

        var workspace = await ResolveCompanyWorkspaceAsync(
            route.AppUserId,
            route.CompanySlug,
            WriteAllowedRoleCodes,
            cancellationToken);
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

        var workspace = await ResolveCompanyWorkspaceAsync(
            route.AppUserId,
            route.CompanySlug,
            WriteAllowedRoleCodes,
            cancellationToken);
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
        var workspace = await ResolveCompanyWorkspaceAsync(
            route.AppUserId,
            route.CompanySlug,
            DeleteAllowedRoleCodes,
            cancellationToken);
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

        var hasDependencies = await _uow.Tickets.HasDeleteDependenciesAsync(
            route.TicketId,
            workspace.Value.ManagementCompanyId,
            cancellationToken);
        if (hasDependencies)
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

    public async Task<Result<TicketTransitionAvailabilityModel>> GetTransitionAvailabilityAsync(
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
            return Result.Fail<TicketTransitionAvailabilityModel>(workspace.Errors);
        }

        var ticket = await _uow.Tickets.FindDetailsAsync(
            route.TicketId,
            workspace.Value.ManagementCompanyId,
            cancellationToken);
        if (ticket is null)
        {
            return Result.Fail<TicketTransitionAvailabilityModel>(new NotFoundError(T("TicketNotFound", "Ticket was not found.")));
        }

        return Result.Ok(await BuildTransitionAvailabilityAsync(
            ticket,
            workspace.Value.ManagementCompanyId,
            cancellationToken));
    }

    public async Task<Result<TicketBllDto>> AdvanceStatusAsync(
        TicketRoute route,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveCompanyWorkspaceAsync(
            route.AppUserId,
            route.CompanySlug,
            WriteAllowedRoleCodes,
            cancellationToken);
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

        var availability = await BuildTransitionAvailabilityAsync(
            ticket,
            managementCompanyId,
            cancellationToken);
        if (!availability.CanAdvance)
        {
            return Result.Fail(availability.BlockingReasons
                .Select(reason => new BusinessRuleError(reason))
                .ToList());
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

    private async Task<TicketTransitionAvailabilityModel> BuildTransitionAvailabilityAsync(
        TicketDetailsDalDto ticket,
        Guid managementCompanyId,
        CancellationToken cancellationToken)
    {
        var nextStatusCode = GetNextStatusCode(ticket.StatusCode);
        if (nextStatusCode is null)
        {
            return new TicketTransitionAvailabilityModel
            {
                TicketId = ticket.Id,
                CurrentStatusCode = ticket.StatusCode,
                CanAdvance = false,
                BlockingReasons =
                [
                    T("TicketAlreadyAtFinalStatus", "Ticket is already at the final status.")
                ]
            };
        }

        var statuses = await GetStatusesAsync(cancellationToken);
        var nextStatus = statuses.FirstOrDefault(status => status.Code == nextStatusCode);
        var blockingReasons = await GetTransitionBlockingReasonsAsync(
            ticket,
            managementCompanyId,
            nextStatusCode,
            cancellationToken);

        return new TicketTransitionAvailabilityModel
        {
            TicketId = ticket.Id,
            CurrentStatusCode = ticket.StatusCode,
            NextStatusCode = nextStatus?.Code ?? nextStatusCode,
            NextStatusLabel = nextStatus?.Label,
            CanAdvance = blockingReasons.Count == 0,
            BlockingReasons = blockingReasons
        };
    }

    private async Task<IReadOnlyList<string>> GetTransitionBlockingReasonsAsync(
        TicketDetailsDalDto ticket,
        Guid managementCompanyId,
        string nextStatusCode,
        CancellationToken cancellationToken)
    {
        var blockingReasons = new List<string>();

        if (string.Equals(nextStatusCode, TicketWorkflowConstants.Assigned, StringComparison.OrdinalIgnoreCase)
            && !ticket.VendorId.HasValue)
        {
            blockingReasons.Add(T(
                "TicketAssignedRequiresVendor",
                "Assign a vendor before moving the ticket to assigned."));
        }

        if (string.Equals(nextStatusCode, TicketWorkflowConstants.Scheduled, StringComparison.OrdinalIgnoreCase)
            && !await _uow.ScheduledWorks.ExistsForTicketAsync(ticket.Id, managementCompanyId, cancellationToken))
        {
            blockingReasons.Add(T(
                "TicketScheduledRequiresScheduledWork",
                "Schedule vendor work before moving the ticket to scheduled."));
        }

        if (string.Equals(nextStatusCode, TicketWorkflowConstants.InProgress, StringComparison.OrdinalIgnoreCase)
            && !await _uow.ScheduledWorks.AnyStartedForTicketAsync(ticket.Id, managementCompanyId, cancellationToken))
        {
            blockingReasons.Add(T(
                "TicketInProgressRequiresStartedWork",
                "Start scheduled work before moving the ticket to in progress."));
        }

        if (string.Equals(nextStatusCode, TicketWorkflowConstants.Completed, StringComparison.OrdinalIgnoreCase)
            && !await _uow.ScheduledWorks.AnyCompletedForTicketAsync(ticket.Id, managementCompanyId, cancellationToken))
        {
            blockingReasons.Add(T(
                "TicketCompletedRequiresCompletedWork",
                "Complete scheduled work before moving the ticket to completed."));
        }

        if (string.Equals(nextStatusCode, TicketWorkflowConstants.Completed, StringComparison.OrdinalIgnoreCase)
            && !await _uow.WorkLogs.ExistsForTicketAsync(ticket.Id, managementCompanyId, cancellationToken))
        {
            blockingReasons.Add(T(
                "TicketCompletedRequiresWorkLog",
                "Add at least one work log before moving the ticket to completed."));
        }

        return blockingReasons;
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

    private sealed record NormalizedTicket(
        string TicketNr,
        string Title,
        string Description);
}
