# Work Log Workflow Plan

## Purpose

Implement work logs for scheduled work. Work logs capture actual labor, time, costs, and descriptions.

## Current state

`WorkLog` exists with created time, work start/end, hours, material cost, labor cost, description, app user, and scheduled work. No service or MVC workflow exists.

## End state

The completed workflow supports:

- list work logs for scheduled work
- add work logs
- edit work logs
- delete where allowed
- view labor/material totals
- use work logs as ticket completion prerequisite

## Domain plan

- No domain changes expected.
- Confirm numeric ranges are appropriate.
- Confirm Description uses LangStr?.

## DTO strategy

Canonical DTO pair:

```text
WorkLogBllDto / WorkLogDalDto
```

Canonical fields:

```csharp
public class CanonicalDto : BaseEntity
{
    public Guid ScheduledWorkId;
    public Guid AppUserId;
    public DateTime? WorkStart;
    public DateTime? WorkEnd;
    public decimal? Hours;
    public decimal? MaterialCost;
    public decimal? LaborCost;
    public string? Description;
}
```

Projection/page models are allowed for list, profile, details, form, totals, or selector pages. Do not add create/update/delete DTOs unless the canonical DTO cannot represent the operation cleanly.

## DAL plan

Repository contract:

```text
IWorkLogRepository : IBaseRepository<WorkLogDalDto>
```

Repository methods to add or confirm:

```csharp
Task /* ... */ AllByScheduledWorkAsync(scheduledWorkId, managementCompanyId);
Task /* ... */ FindInCompanyAsync(workLogId, managementCompanyId);
Task /* ... */ ExistsInCompanyAsync(workLogId, managementCompanyId);
Task /* ... */ ExistsForScheduledWorkAsync(scheduledWorkId, managementCompanyId);
Task /* ... */ ExistsForTicketAsync(ticketId, managementCompanyId);
Task /* ... */ TotalsForScheduledWorkAsync(scheduledWorkId, managementCompanyId);
Task /* ... */ TotalsForTicketAsync(ticketId, managementCompanyId);
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
IWorkLogService : IBaseService<WorkLogBllDto>
```

ITicketService methods:

```csharp
Task<Result> ListForScheduledWorkAsync(ScheduledWorkRoute route);
Task<Result> AddAsync(ScheduledWorkRoute route, WorkLogBllDto dto);
Task<Result> UpdateAsync(WorkLogRoute route, WorkLogBllDto dto);
Task<Result> DeleteAsync(WorkLogRoute route);
```

Service implementation requirements:

- Implement inside `TicketService` or an internal helper composed by `TicketService`.
- If an internal helper is used, it may inherit `BaseService<WorkLogBllDto, WorkLogDalDto, IWorkLogRepository, IAppUOW>`.
- Do not expose the helper through `IAppBLL`.
- Resolve current company/parent context from route.
- Check active user role or resident/customer context.
- Validate IDs through tenant-safe repository methods.
- Normalize strings before persistence.
- Return existing BLL error types: `UnauthorizedError`, `ForbiddenError`, `NotFoundError`, `ValidationAppError`, `ConflictError`, `BusinessRuleError`.
- Save changes through UOW.

## Business rules

- Scheduled work must belong to company.
- Add/update roles: OWNER, MANAGER, SUPPORT.
- Cost visibility: OWNER, MANAGER, FINANCE.
- Do not trust dto.AppUserId; set from current actor.
- Hours/costs non-negative.
- WorkEnd cannot be before WorkStart.
- At least one meaningful field required.
- Block edit/delete after ticket closed unless explicit manager override.

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

- Add WorkLogsController.
- Add index/form/delete ViewModels.
- Add Index, Create, Edit, Delete views.
- Scheduled work details embeds log summary and totals.

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

- Entry point: Scheduled Work Details -> Work logs.

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

- Work logs can be added/edited/deleted where allowed.
- Totals available.
- Ticket completion can require work logs.
- No API controller.
