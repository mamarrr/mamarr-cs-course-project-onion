using App.BLL.Contracts.Common;
using App.BLL.Contracts.Common.Errors;
using App.BLL.Contracts.Customers.Queries;
using App.BLL.Contracts.Customers.Services;
using App.BLL.Contracts.Tickets;
using App.BLL.Contracts.Tickets.Commands;
using App.BLL.Contracts.Tickets.Models;
using App.BLL.Contracts.Tickets.Queries;
using App.BLL.Contracts.Tickets.Services;
using App.DAL.Contracts;
using App.DAL.Contracts.DAL.Tickets;
using Base.Domain;
using FluentResults;

namespace App.BLL.Tickets;

public class ManagementTicketService : IManagementTicketService
{
    private const int TicketNrMaxLength = 20;

    private readonly ICustomerAccessService _customerAccessService;
    private readonly IAppUOW _uow;

    public ManagementTicketService(
        ICustomerAccessService customerAccessService,
        IAppUOW uow)
    {
        _customerAccessService = customerAccessService;
        _uow = uow;
    }

    public async Task<Result<ManagementTicketsModel>> GetTicketsAsync(
        GetManagementTicketsQuery query,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveCompanyAsync(query.UserId, query.CompanySlug, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        var filter = ToFilterDto(query);
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
                Search = query.Search,
                StatusId = query.StatusId,
                PriorityId = query.PriorityId,
                CategoryId = query.CategoryId,
                CustomerId = query.CustomerId,
                PropertyId = query.PropertyId,
                UnitId = query.UnitId,
                VendorId = query.VendorId,
                DueFrom = query.DueFrom,
                DueTo = query.DueTo
            },
            Options = await BuildOptionsAsync(
                workspace.Value.ManagementCompanyId,
                query.CustomerId,
                query.PropertyId,
                query.UnitId,
                query.CategoryId,
                cancellationToken)
        });
    }

    public async Task<Result<ManagementTicketDetailsModel>> GetDetailsAsync(
        GetManagementTicketQuery query,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveCompanyAsync(query.UserId, query.CompanySlug, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        var ticket = await _uow.Tickets.FindDetailsAsync(
            query.TicketId,
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
            NextStatusLabel = nextStatus?.Label
        });
    }

    public async Task<Result<ManagementTicketFormModel>> GetCreateFormAsync(
        GetManagementTicketsQuery query,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveCompanyAsync(query.UserId, query.CompanySlug, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        var options = await BuildOptionsAsync(
            workspace.Value.ManagementCompanyId,
            query.CustomerId,
            query.PropertyId,
            query.UnitId,
            query.CategoryId,
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
            CustomerId = query.CustomerId,
            PropertyId = query.PropertyId,
            UnitId = query.UnitId,
            Options = options
        });
    }

    public async Task<Result<ManagementTicketFormModel>> GetEditFormAsync(
        GetManagementTicketQuery query,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveCompanyAsync(query.UserId, query.CompanySlug, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        var ticket = await _uow.Tickets.FindForEditAsync(
            query.TicketId,
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
        GetManagementTicketSelectorOptionsQuery query,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveCompanyAsync(query.UserId, query.CompanySlug, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        return Result.Ok(await BuildOptionsAsync(
            workspace.Value.ManagementCompanyId,
            query.CustomerId,
            query.PropertyId,
            query.UnitId,
            query.CategoryId,
            cancellationToken));
    }

    public async Task<Result<Guid>> CreateAsync(
        CreateManagementTicketCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateCreate(command);
        if (validation.IsFailed)
        {
            return Result.Fail(validation.Errors);
        }

        var workspace = await ResolveCompanyAsync(command.UserId, command.CompanySlug, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        var createdStatus = await _uow.Tickets.FindStatusByCodeAsync(
            TicketWorkflowConstants.Created,
            cancellationToken);
        if (createdStatus is null)
        {
            return Result.Fail(new BusinessRuleError(T("TicketCreatedStatusMissing", "Created ticket status is not configured.")));
        }

        var referenceValidation = await ValidateReferencesAsync(
            workspace.Value.ManagementCompanyId,
            command.TicketCategoryId,
            command.TicketPriorityId,
            createdStatus.Id,
            command.CustomerId,
            command.PropertyId,
            command.UnitId,
            command.ResidentId,
            command.VendorId,
            requireCascadingParents: true,
            cancellationToken);

        if (referenceValidation.IsFailed)
        {
            return Result.Fail(referenceValidation.Errors);
        }

        var normalized = NormalizeCreate(command);
        var duplicate = await _uow.Tickets.TicketNrExistsAsync(
            workspace.Value.ManagementCompanyId,
            normalized.TicketNr,
            cancellationToken: cancellationToken);

        if (duplicate)
        {
            return Result.Fail(new ConflictError(T("TicketNumberAlreadyExists", "Ticket number already exists in this company.")));
        }

        var ticketId = await _uow.Tickets.AddAsync(
            new TicketCreateDalDto
            {
                ManagementCompanyId = workspace.Value.ManagementCompanyId,
                TicketNr = normalized.TicketNr,
                Title = new LangStr(normalized.Title, normalized.Culture),
                Description = new LangStr(normalized.Description, normalized.Culture),
                TicketCategoryId = command.TicketCategoryId,
                TicketStatusId = createdStatus.Id,
                TicketPriorityId = command.TicketPriorityId,
                CustomerId = command.CustomerId,
                PropertyId = command.PropertyId,
                UnitId = command.UnitId,
                ResidentId = command.ResidentId,
                VendorId = command.VendorId,
                DueAt = command.DueAt
            },
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Ok(ticketId);
    }

    public async Task<Result> UpdateAsync(
        UpdateManagementTicketCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateUpdate(command);
        if (validation.IsFailed)
        {
            return Result.Fail(validation.Errors);
        }

        var workspace = await ResolveCompanyAsync(command.UserId, command.CompanySlug, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        var existing = await _uow.Tickets.FindForEditAsync(
            command.TicketId,
            workspace.Value.ManagementCompanyId,
            cancellationToken);
        if (existing is null)
        {
            return Result.Fail(new NotFoundError(T("TicketNotFound", "Ticket was not found.")));
        }

        var targetStatus = await _uow.Tickets.FindStatusByIdAsync(command.TicketStatusId, cancellationToken);
        if (targetStatus is null)
        {
            return Result.Fail(new ValidationAppError("Validation failed.", [
                new ValidationFailureModel
                {
                    PropertyName = nameof(command.TicketStatusId),
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
            command.TicketCategoryId,
            command.TicketPriorityId,
            command.TicketStatusId,
            command.CustomerId,
            command.PropertyId,
            command.UnitId,
            command.ResidentId,
            command.VendorId,
            requireCascadingParents: true,
            cancellationToken);

        if (referenceValidation.IsFailed)
        {
            return Result.Fail(referenceValidation.Errors);
        }

        var statusGuard = ValidateStatusGuard(targetStatus.Code, command.VendorId, command.DueAt);
        if (statusGuard.IsFailed)
        {
            return Result.Fail(statusGuard.Errors);
        }

        var normalized = NormalizeUpdate(command);
        var duplicate = await _uow.Tickets.TicketNrExistsAsync(
            workspace.Value.ManagementCompanyId,
            normalized.TicketNr,
            command.TicketId,
            cancellationToken);

        if (duplicate)
        {
            return Result.Fail(new ConflictError(T("TicketNumberAlreadyExists", "Ticket number already exists in this company.")));
        }

        var updated = await _uow.Tickets.UpdateAsync(
            new TicketUpdateDalDto
            {
                Id = command.TicketId,
                ManagementCompanyId = workspace.Value.ManagementCompanyId,
                TicketNr = normalized.TicketNr,
                Title = normalized.Title,
                Description = normalized.Description,
                Culture = normalized.Culture,
                TicketCategoryId = command.TicketCategoryId,
                TicketStatusId = command.TicketStatusId,
                TicketPriorityId = command.TicketPriorityId,
                CustomerId = command.CustomerId,
                PropertyId = command.PropertyId,
                UnitId = command.UnitId,
                ResidentId = command.ResidentId,
                VendorId = command.VendorId,
                DueAt = command.DueAt,
                ClosedAt = targetStatus.Code == TicketWorkflowConstants.Closed ? DateTime.UtcNow : null
            },
            cancellationToken);

        if (!updated)
        {
            return Result.Fail(new NotFoundError(T("TicketNotFound", "Ticket was not found.")));
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(
        DeleteManagementTicketCommand command,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveCompanyAsync(command.UserId, command.CompanySlug, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        var deleted = await _uow.Tickets.DeleteAsync(
            command.TicketId,
            workspace.Value.ManagementCompanyId,
            cancellationToken);

        if (!deleted)
        {
            return Result.Fail(new NotFoundError(T("TicketNotFound", "Ticket was not found.")));
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> AdvanceStatusAsync(
        AdvanceManagementTicketStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveCompanyAsync(command.UserId, command.CompanySlug, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        var ticket = await _uow.Tickets.FindDetailsAsync(
            command.TicketId,
            workspace.Value.ManagementCompanyId,
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

        var statusGuard = ValidateStatusGuard(nextStatusCode, ticket.VendorId, ticket.DueAt);
        if (statusGuard.IsFailed)
        {
            return Result.Fail(statusGuard.Errors);
        }

        var nextStatus = await _uow.Tickets.FindStatusByCodeAsync(nextStatusCode, cancellationToken);
        if (nextStatus is null)
        {
            return Result.Fail(new BusinessRuleError(T("TicketNextStatusMissing", "Next ticket status is not configured.")));
        }

        var updated = await _uow.Tickets.UpdateStatusAsync(
            new TicketStatusUpdateDalDto
            {
                Id = command.TicketId,
                ManagementCompanyId = workspace.Value.ManagementCompanyId,
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

    private async Task<Result<App.BLL.Contracts.Customers.Models.CompanyWorkspaceModel>> ResolveCompanyAsync(
        Guid userId,
        string companySlug,
        CancellationToken cancellationToken)
    {
        return await _customerAccessService.ResolveCompanyWorkspaceAsync(
            new GetCompanyCustomersQuery
            {
                UserId = userId,
                CompanySlug = companySlug
            },
            cancellationToken);
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
            Priorities = (await _uow.Tickets.AllPrioritiesAsync(cancellationToken)).Select(MapOption).ToList(),
            Categories = (await _uow.Tickets.AllCategoriesAsync(cancellationToken)).Select(MapOption).ToList(),
            Customers = (await _uow.Tickets.CustomerOptionsAsync(managementCompanyId, cancellationToken)).Select(MapOption).ToList(),
            Properties = (await _uow.Tickets.PropertyOptionsAsync(managementCompanyId, customerId, cancellationToken)).Select(MapOption).ToList(),
            Units = (await _uow.Tickets.UnitOptionsAsync(managementCompanyId, propertyId, cancellationToken)).Select(MapOption).ToList(),
            Residents = (await _uow.Tickets.ResidentOptionsAsync(managementCompanyId, unitId, cancellationToken)).Select(MapOption).ToList(),
            Vendors = (await _uow.Tickets.VendorOptionsAsync(managementCompanyId, categoryId, cancellationToken)).Select(MapOption).ToList()
        };
    }

    private async Task<IReadOnlyList<TicketOptionModel>> GetStatusesAsync(CancellationToken cancellationToken)
    {
        var statuses = (await _uow.Tickets.AllStatusesAsync(cancellationToken))
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
        var references = await _uow.Tickets.ValidateReferencesAsync(
            managementCompanyId,
            categoryId,
            priorityId,
            statusId,
            customerId,
            propertyId,
            unitId,
            residentId,
            vendorId,
            cancellationToken);

        var failures = new List<ValidationFailureModel>();

        if (!references.CategoryExists)
        {
            failures.Add(Failure("TicketCategoryId", T("InvalidTicketCategory", "Selected ticket category is invalid.")));
        }

        if (!references.PriorityExists)
        {
            failures.Add(Failure("TicketPriorityId", T("InvalidTicketPriority", "Selected ticket priority is invalid.")));
        }

        if (!references.StatusExists)
        {
            failures.Add(Failure("TicketStatusId", T("InvalidTicketStatus", "Selected ticket status is invalid.")));
        }

        if (!references.CustomerBelongsToCompany)
        {
            failures.Add(Failure("CustomerId", T("InvalidTicketCustomer", "Selected customer is invalid.")));
        }

        if (requireCascadingParents && propertyId.HasValue && !customerId.HasValue)
        {
            failures.Add(Failure("CustomerId", T("TicketCustomerRequiredForProperty", "Select a customer before selecting a property.")));
        }

        if (!references.PropertyBelongsToCompany || !references.PropertyBelongsToCustomer)
        {
            failures.Add(Failure("PropertyId", T("InvalidTicketProperty", "Selected property is invalid for this customer.")));
        }

        if (requireCascadingParents && unitId.HasValue && !propertyId.HasValue)
        {
            failures.Add(Failure("PropertyId", T("TicketPropertyRequiredForUnit", "Select a property before selecting a unit.")));
        }

        if (!references.UnitBelongsToCompany || !references.UnitBelongsToProperty)
        {
            failures.Add(Failure("UnitId", T("InvalidTicketUnit", "Selected unit is invalid for this property.")));
        }

        if (!references.ResidentBelongsToCompany || !references.ResidentLinkedToUnit)
        {
            failures.Add(Failure("ResidentId", T("InvalidTicketResident", "Selected resident is invalid for this unit.")));
        }

        if (!references.VendorBelongsToCompany)
        {
            failures.Add(Failure("VendorId", T("InvalidTicketVendor", "Selected vendor is invalid.")));
        }

        return failures.Count == 0
            ? Result.Ok()
            : Result.Fail(new ValidationAppError("Validation failed.", failures));
    }

    private static Result ValidateCreate(CreateManagementTicketCommand command)
    {
        var failures = new List<ValidationFailureModel>();

        AddRequired(failures, nameof(command.TicketNr), command.TicketNr, T("TicketNumber", "Ticket number"));
        AddRequired(failures, nameof(command.Title), command.Title, T("Title", "Title"));
        AddRequired(failures, nameof(command.Description), command.Description, T("Description", "Description"));

        if (!string.IsNullOrWhiteSpace(command.TicketNr) && command.TicketNr.Trim().Length > TicketNrMaxLength)
        {
            failures.Add(Failure(nameof(command.TicketNr), T("TicketNumberMaxLength", "Ticket number must be 20 characters or fewer.")));
        }

        if (command.TicketCategoryId == Guid.Empty)
        {
            failures.Add(Failure(nameof(command.TicketCategoryId), T("TicketCategoryRequired", "Ticket category is required.")));
        }

        if (command.TicketPriorityId == Guid.Empty)
        {
            failures.Add(Failure(nameof(command.TicketPriorityId), T("TicketPriorityRequired", "Ticket priority is required.")));
        }

        return failures.Count == 0
            ? Result.Ok()
            : Result.Fail(new ValidationAppError("Validation failed.", failures));
    }

    private static Result ValidateUpdate(UpdateManagementTicketCommand command)
    {
        var failures = new List<ValidationFailureModel>();

        AddRequired(failures, nameof(command.TicketNr), command.TicketNr, T("TicketNumber", "Ticket number"));
        AddRequired(failures, nameof(command.Title), command.Title, T("Title", "Title"));
        AddRequired(failures, nameof(command.Description), command.Description, T("Description", "Description"));

        if (!string.IsNullOrWhiteSpace(command.TicketNr) && command.TicketNr.Trim().Length > TicketNrMaxLength)
        {
            failures.Add(Failure(nameof(command.TicketNr), T("TicketNumberMaxLength", "Ticket number must be 20 characters or fewer.")));
        }

        if (command.TicketCategoryId == Guid.Empty)
        {
            failures.Add(Failure(nameof(command.TicketCategoryId), T("TicketCategoryRequired", "Ticket category is required.")));
        }

        if (command.TicketStatusId == Guid.Empty)
        {
            failures.Add(Failure(nameof(command.TicketStatusId), T("TicketStatusRequired", "Ticket status is required.")));
        }

        if (command.TicketPriorityId == Guid.Empty)
        {
            failures.Add(Failure(nameof(command.TicketPriorityId), T("TicketPriorityRequired", "Ticket priority is required.")));
        }

        return failures.Count == 0
            ? Result.Ok()
            : Result.Fail(new ValidationAppError("Validation failed.", failures));
    }

    private static Result ValidateStatusGuard(string statusCode, Guid? vendorId, DateTime? dueAt)
    {
        if (!IsScheduledOrLater(statusCode) || vendorId.HasValue && dueAt.HasValue)
        {
            return Result.Ok();
        }

        return Result.Fail(new ValidationAppError("Validation failed.", [
            Failure("VendorId", T("TicketScheduledRequiresVendor", "Scheduled or later tickets require a vendor.")),
            Failure("DueAt", T("TicketScheduledRequiresDueDate", "Scheduled or later tickets require a due date."))
        ]));
    }

    private static TicketListFilterDalDto ToFilterDto(GetManagementTicketsQuery query)
    {
        return new TicketListFilterDalDto
        {
            Search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(),
            StatusId = query.StatusId,
            PriorityId = query.PriorityId,
            CategoryId = query.CategoryId,
            CustomerId = query.CustomerId,
            PropertyId = query.PropertyId,
            UnitId = query.UnitId,
            VendorId = query.VendorId,
            DueFrom = query.DueFrom,
            DueTo = query.DueTo
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

    private static NormalizedCreate NormalizeCreate(CreateManagementTicketCommand command)
    {
        return new NormalizedCreate(
            command.TicketNr.Trim(),
            command.Title.Trim(),
            command.Description.Trim(),
            NormalizeCulture(command.Culture));
    }

    private static NormalizedUpdate NormalizeUpdate(UpdateManagementTicketCommand command)
    {
        return new NormalizedUpdate(
            command.TicketNr.Trim(),
            command.Title.Trim(),
            command.Description.Trim(),
            NormalizeCulture(command.Culture));
    }

    private static string NormalizeCulture(string culture)
    {
        return string.IsNullOrWhiteSpace(culture)
            ? Thread.CurrentThread.CurrentUICulture.Name
            : culture.Trim();
    }

    private static string T(string key, string fallback)
    {
        return App.Resources.Views.UiText.ResourceManager.GetString(key) ?? fallback;
    }

    private sealed record NormalizedCreate(
        string TicketNr,
        string Title,
        string Description,
        string Culture);

    private sealed record NormalizedUpdate(
        string TicketNr,
        string Title,
        string Description,
        string Culture);
}
