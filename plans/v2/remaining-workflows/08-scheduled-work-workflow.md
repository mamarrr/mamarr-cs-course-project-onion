# Scheduled Work Workflow Plan

## Purpose

Implement scheduled work as the operational execution workflow between tickets and vendor work.

## Current state

`ScheduledWork` exists with planned/actual start/end, notes, vendor, ticket, work status, and work logs. No service or MVC workflow exists.

## End state

The completed workflow supports:

- list scheduled work for ticket
- schedule vendor work
- update schedule details
- start work
- complete work
- cancel work
- delete when no logs exist
- use scheduled work as ticket transition prerequisite

## Domain plan

- No domain changes expected.
- Confirm WorkStatus lookup seed codes: PLANNED, IN_PROGRESS, COMPLETED, CANCELLED.
- Confirm indexes for vendor schedule and ticket schedule queries.

## DTO strategy

Canonical DTO pair:

```text
ScheduledWorkBllDto / ScheduledWorkDalDto
```

Canonical fields:

```csharp
public class CanonicalDto : BaseEntity
{
    public Guid VendorId;
    public Guid TicketId;
    public Guid WorkStatusId;
    public DateTime ScheduledStart;
    public DateTime? ScheduledEnd;
    public DateTime? RealStart;
    public DateTime? RealEnd;
    public string? Notes;
}
```

Projection/page models are allowed for list, profile, details, form, totals, or selector pages. Do not add create/update/delete DTOs unless the canonical DTO cannot represent the operation cleanly.

## DAL plan

Repository contract:

```text
IScheduledWorkRepository : IBaseRepository<ScheduledWorkDalDto>
```

Repository methods to add or confirm:

```csharp
Task /* ... */ AllByCompanyAsync(managementCompanyId, filter);
Task /* ... */ AllByTicketAsync(ticketId, managementCompanyId);
Task /* ... */ FindDetailsAsync(scheduledWorkId, managementCompanyId);
Task /* ... */ FindInCompanyAsync(scheduledWorkId, managementCompanyId);
Task /* ... */ ExistsForTicketAsync(ticketId, managementCompanyId);
Task /* ... */ HasWorkLogsAsync(scheduledWorkId, managementCompanyId);
Task /* ... */ VendorBelongsToTicketCompanyAsync(vendorId, ticketId, managementCompanyId);
Task /* ... */ VendorSupportsTicketCategoryAsync(vendorId, ticketId);
Task /* ... */ AnyStartedForTicketAsync(ticketId, managementCompanyId);
Task /* ... */ AnyCompletedForTicketAsync(ticketId, managementCompanyId);
```

Repository implementation requirements:

- Place implementation under `App.DAL.EF/Repositories`.
- Inherit from `BaseRepository<TDalDto, TDomain, AppDbContext>` for persisted entities.
- Apply tenant scoping in every read and mutation.
- Use `AsNoTracking()` for reads.
- Use `AsTracking()` for updates.
- Override `UpdateAsync` where `LangStr`, parent filtering, or relationship-safe updates are needed.
- Return DAL DTOs or DAL projection DTOs only.

## UOW wiring

Update:

```text
App.DAL.Contracts/IAppUow.cs
App.DAL.EF/AppUOW.cs
```

Steps:

1. Add repository property to `IAppUOW`.
2. Add mapper field to `AppUOW`.
3. Add private lazy repository field.
4. Add lazy property implementation.

## BLL plan

Service contract:

```text
IScheduledWorkService : IBaseService<ScheduledWorkBllDto>
```

ITicketService methods:

```csharp
Task<Result> ListForTicketAsync(TicketRoute route);
Task<Result> GetDetailsAsync(ScheduledWorkRoute route);
Task<Result> GetCreateFormAsync(TicketRoute route);
Task<Result> GetEditFormAsync(ScheduledWorkRoute route);
Task<Result> ScheduleAsync(TicketRoute route, ScheduledWorkBllDto dto);
Task<Result> UpdateScheduleAsync(ScheduledWorkRoute route, ScheduledWorkBllDto dto);
Task<Result> StartWorkAsync(ScheduledWorkRoute route, DateTime realStart);
Task<Result> CompleteWorkAsync(ScheduledWorkRoute route, DateTime realEnd);
Task<Result> CancelAsync(ScheduledWorkRoute route);
Task<Result> DeleteAsync(ScheduledWorkRoute route);
```

Service implementation requirements:

- Implement inside `TicketService` or an internal helper composed by `TicketService`.
- If an internal helper is used, it may inherit `BaseService<ScheduledWorkBllDto, ScheduledWorkDalDto, IScheduledWorkRepository, IAppUOW>`.
- Do not expose the helper through `IAppBLL`.
- Resolve current company/parent context from route.
- Check active user role or resident/customer context.
- Validate IDs through tenant-safe repository methods.
- Normalize strings before persistence.
- Return existing BLL error types: `UnauthorizedError`, `ForbiddenError`, `NotFoundError`, `ValidationAppError`, `ConflictError`, `BusinessRuleError`.
- Save changes through UOW.

## Business rules

- Ticket must belong to company.
- Vendor must belong to company.
- Vendor should support ticket category if ticket has category.
- Work status must exist.
- Scheduling allowed for OWNER, MANAGER, SUPPORT.
- ScheduledEnd cannot be before ScheduledStart.
- RealEnd cannot be before RealStart.
- Delete blocked if work logs exist.

## AppBLL wiring

Update:

```text
App.BLL.Contracts/IAppBLL.cs
App.BLL/AppBLL.cs
```

Steps:

1. Add service property to `IAppBLL`.
2. Add private lazy service field to `AppBLL`.
3. Instantiate service with UOW and dependent services.
4. Avoid circular dependencies.

## WebApp MVC plan

- Add ScheduledWorksController.
- Add index/form/details/action/delete ViewModels.
- Add Index, Details, Create, Edit, Delete views.
- Ticket details embeds scheduled work summary.

Controller rules:

- Controllers are thin adapters.
- Controllers build route models from URL values and current user ID.
- Controllers map ViewModels to canonical BLL DTOs.
- Controllers do not duplicate tenant/RBAC/lifecycle rules.
- POST actions use PRG on success.
- Add BLL validation failures to `ModelState`.

## Razor views

Add views under the relevant Management area folder.

Typical view set:

```text
Index.cshtml
Details.cshtml
Create.cshtml
Edit.cshtml
Delete.cshtml
```

Nested workflows may use fewer views if embedded into parent pages.

## Navigation

- Entry point: Ticket Details -> Scheduled work.

## Localization

Add resource entries for:

- page titles,
- form labels,
- validation messages,
- success messages,
- delete confirmations,
- lifecycle blocking reasons where applicable.

Add English and Estonian entries together.

## Definition of done

- Tickets can have scheduled work records.
- Start/complete/cancel actions exist.
- Ticket lifecycle can check scheduled work.
- No API controller.
