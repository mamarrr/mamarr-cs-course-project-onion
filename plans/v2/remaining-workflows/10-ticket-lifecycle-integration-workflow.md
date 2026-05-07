# Ticket Lifecycle Integration Workflow Plan

## Purpose

Tighten ticket lifecycle transitions so they depend on real workflow records: vendor assignment, scheduled work, work progress, and work logs.

## Current state

Ticket service supports basic CRUD, details, search, and AdvanceStatusAsync. It currently validates basic vendor/due date guards but does not require ScheduledWork or WorkLog records.

## End state

The completed workflow supports:

- CREATED -> ASSIGNED requires vendor
- ASSIGNED -> SCHEDULED requires scheduled work
- SCHEDULED -> IN_PROGRESS requires actual start
- IN_PROGRESS -> COMPLETED requires completed scheduled work and work logs
- COMPLETED -> CLOSED remains resolution verification

## Domain plan

- No domain changes expected.
- Confirm status codes: CREATED, ASSIGNED, SCHEDULED, IN_PROGRESS, COMPLETED, CLOSED.

## DTO strategy

Canonical DTO pair:

```text
TicketTransitionAvailabilityModel; no new persisted canonical DTO.
```

Canonical fields:

```csharp
public class CanonicalDto : BaseEntity
{
    public Guid TicketId;
    public string CurrentStatusCode;
    public string? NextStatusCode;
    public string? NextStatusLabel;
    public bool CanAdvance;
    public IReadOnlyList<string> BlockingReasons;
}
```

Projection/page models are allowed for list, profile, details, form, totals, or selector pages. Do not add create/update/delete DTOs unless the canonical DTO cannot represent the operation cleanly.

## DAL plan

Repository contract:

```text
Use ITicketRepository plus IScheduledWorkRepository and IWorkLogRepository.
```

Repository methods to add or confirm:

```csharp
Task /* ... */ TicketRepository.GetTransitionStateAsync(ticketId, managementCompanyId) optional;
Task /* ... */ ScheduledWorks.ExistsForTicketAsync(ticketId, managementCompanyId);
Task /* ... */ ScheduledWorks.AnyStartedForTicketAsync(ticketId, managementCompanyId);
Task /* ... */ ScheduledWorks.AnyCompletedForTicketAsync(ticketId, managementCompanyId);
Task /* ... */ WorkLogs.ExistsForTicketAsync(ticketId, managementCompanyId);
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
Extend ITicketService
```

Service methods:

```csharp
Task<Result> GetTransitionAvailabilityAsync(TicketRoute route);
Task<Result> AdvanceStatusAsync(TicketRoute route) with stronger prerequisites;
```

Service implementation requirements:

- Place implementation under `App.BLL/Services`.
- Inherit from `BaseService<TBllDto, TDalDto, TRepository, IAppUOW>` where there is a persisted entity.
- Resolve current company/parent context from route.
- Check active management-company role only. Do not allow customer or resident context access for ticket lifecycle transitions.
- Validate IDs through tenant-safe repository methods.
- Normalize strings before persistence.
- Return existing BLL error types: `UnauthorizedError`, `ForbiddenError`, `NotFoundError`, `ValidationAppError`, `ConflictError`, `BusinessRuleError`.
- Save changes through UOW.

## Business rules

- CREATED -> ASSIGNED requires vendor.
- ASSIGNED -> SCHEDULED requires scheduled work.
- SCHEDULED -> IN_PROGRESS requires started scheduled work.
- IN_PROGRESS -> COMPLETED requires completed scheduled work and work logs.
- COMPLETED -> CLOSED allowed for management roles until verification workflow is added.
- Read/list/details allowed for OWNER, MANAGER, FINANCE, SUPPORT.
- Advancing lifecycle state allowed for OWNER, MANAGER, SUPPORT unless a stricter transition rule is added.
- Customer context and resident context are not allowed to advance ticket lifecycle states.
- Blocking reasons returned through BusinessRuleError or availability model.

## AppBLL wiring

No new `IAppBLL` property is required.

Update:

```text
App.BLL.Contracts/Tickets/ITicketService.cs
App.BLL/Services/Tickets/TicketService.cs
```

Steps:

1. Extend existing `ITicketService` methods for transition availability and stronger `AdvanceStatusAsync` prerequisites.
2. Implement lifecycle checks in `TicketService`, using `IScheduledWorkRepository` and `IWorkLogRepository` through `IAppUOW`.
3. Do not add a new `IAppBLL` property or separate lifecycle facade.
4. Avoid circular dependencies.

## WebApp MVC plan

- Update Ticket Details ViewModel/page to show transition availability and blocking reasons.
- Update AdvanceStatus POST to show blocking reasons.
- Add links from ticket details to schedule work and work logs.
- Scheduled-work start/complete methods on `ITicketService` can update ticket status when prerequisites are met.

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

- Entry point: Ticket Details -> lifecycle actions, scheduled work, work logs.

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

- Ticket advancement enforces real workflow prerequisites.
- Ticket details explains why transition is blocked.
- Scheduled work and work logs are linked.
- No API controller.
