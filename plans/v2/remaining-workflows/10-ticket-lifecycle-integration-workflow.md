# Ticket Lifecycle Integration Workflow Plan

## Purpose

Tighten ticket lifecycle transitions so they depend on real workflow records: vendor assignment, scheduled work, work progress, and work logs.

## Current state

Ticket service supports basic CRUD, details, search, and AdvanceStatusAsync.

Plans 08 and 09 are considered complete. Scheduled work and work log workflows exist and are exposed publicly through `ITicketService / IAppBLL.Tickets`.

`TicketService` has become too large because it currently contains core ticket behavior, scheduled-work behavior, work-log behavior, and lifecycle-transition behavior in one implementation class. Phase 10 must refactor this implementation shape while keeping the same public facade.

## End state

The completed workflow supports:

- CREATED -> ASSIGNED requires vendor
- ASSIGNED -> SCHEDULED requires scheduled work
- SCHEDULED -> IN_PROGRESS requires actual start
- IN_PROGRESS -> COMPLETED requires completed scheduled work and work logs
- COMPLETED -> CLOSED remains resolution verification
- `TicketService` remains the only public controller-facing ticket facade
- scheduled-work behavior is extracted from `TicketService` into `IScheduledWorkService` / `ScheduledWorkService`
- work-log behavior is extracted from `TicketService` into `IWorkLogService` / `WorkLogService`
- controllers still call only `_bll.Tickets`; they do not call `IScheduledWorkService`, `IWorkLogService`, `ScheduledWorkService`, or `WorkLogService` directly

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

## Service extraction plan

Phase 10 must extract the plan 08 and plan 09 implementation code out of `TicketService` without changing the public AppBLL surface.

Add BLL contracts and concrete implementation services:

```text
App.BLL.Contracts/Tickets/IScheduledWorkService.cs
App.BLL.Contracts/Tickets/IWorkLogService.cs
App.BLL/Services/Tickets/ScheduledWorkService.cs
App.BLL/Services/Tickets/WorkLogService.cs
```

These are regular BLL services with contracts, but they are **not** AppBLL facades:

```csharp
public interface IScheduledWorkService :
    IBaseService<ScheduledWorkBllDto>
{
    // scheduled-work workflow methods currently delegated by ITicketService
}

public class ScheduledWorkService :
    BaseService<ScheduledWorkBllDto, ScheduledWorkDalDto, IScheduledWorkRepository, IAppUOW>,
    IScheduledWorkService
{
    // scheduled-work workflow implementation
}

public interface IWorkLogService :
    IBaseService<WorkLogBllDto>
{
    // work-log workflow methods currently delegated by ITicketService
}

public class WorkLogService :
    BaseService<WorkLogBllDto, WorkLogDalDto, IWorkLogRepository, IAppUOW>,
    IWorkLogService
{
    // work-log workflow implementation
}
```

Rules:

- Add `IScheduledWorkService` and `IWorkLogService` under `App.BLL.Contracts`.
- These service contracts are for BLL composition and DI only.
- These services are **not** first-class AppBLL facades.
- Do **not** add `IAppBLL.ScheduledWorks`.
- Do **not** add `IAppBLL.WorkLogs`.
- Register the interfaces with their implementations in DI so they can be injected into `TicketService`.
- `TicketService` wraps/delegates to them and remains the only service exposed through `IAppBLL.Tickets`.
- Controllers call only `IAppBLL.Tickets`.
- Controllers must not depend on `IScheduledWorkService`, `IWorkLogService`, `ScheduledWorkService`, or `WorkLogService` directly.
- The extracted services must follow the same BLL architecture rules as other services: tenant resolution, RBAC, IDOR protection, validation, lifecycle rules, transactions, UOW persistence, BLL DTOs, and FluentResults errors.
- Shared lifecycle logic should be coordinated through `TicketService` or a private/internal lifecycle helper to avoid circular dependencies.
- Extracted services must not depend on `ITicketService`; this avoids a circular dependency between `TicketService` and its delegated services.

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

Service contracts:

```text
Extend ITicketService for the controller-facing public ticket facade.
Add IScheduledWorkService and IWorkLogService as BLL composition contracts behind TicketService.
Do not expose scheduled work or work logs through IAppBLL.
```

Service methods:

```csharp
Task<Result<TicketTransitionAvailabilityModel>> GetTransitionAvailabilityAsync(TicketRoute route);
Task<Result> AdvanceStatusAsync(TicketRoute route) with stronger prerequisites;
```

Delegation shape:

```csharp
public class TicketService : BaseService<TicketBllDto, TicketDalDto, ITicketRepository, IAppUOW>, ITicketService
{
    private readonly IScheduledWorkService _scheduledWorkService;
    private readonly IWorkLogService _workLogService;

    public Task<Result<ScheduledWorkListModel>> ListScheduledWorkForTicketAsync(...)
        => _scheduledWorkService.ListForTicketAsync(...);

    public Task<Result<WorkLogListModel>> ListWorkLogsForScheduledWorkAsync(...)
        => _workLogService.ListForScheduledWorkAsync(...);
}
```

The method names inside `IScheduledWorkService` and `IWorkLogService` can be shorter than the public `ITicketService` names, but MVC/controller-facing access remains on `ITicketService`.

Service implementation requirements:

- Place implementation under `App.BLL/Services`.
- Inherit from `BaseService<TBllDto, TDalDto, TRepository, IAppUOW>` where there is a persisted entity.
- Resolve current company/parent context from route.
- Check active management-company role only. Do not allow customer or resident context access for ticket lifecycle, scheduled work, or work logs.
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
- Blocking reasons returned through BusinessRuleError or availability model.

## AppBLL wiring

No new `IAppBLL` property is required.

Do **not** update `IAppBLL` or `AppBLL` to expose scheduled work or work logs directly.

Update:

```text
App.BLL.Contracts/Tickets/ITicketService.cs
App.BLL.Contracts/Tickets/IScheduledWorkService.cs
App.BLL.Contracts/Tickets/IWorkLogService.cs
App.BLL/Services/Tickets/TicketService.cs
App.BLL/Services/Tickets/ScheduledWorkService.cs
App.BLL/Services/Tickets/WorkLogService.cs
```

Steps:

1. Keep scheduled-work and work-log controller-facing methods on `ITicketService`.
2. Add `IScheduledWorkService` and `IWorkLogService` contracts for BLL composition.
3. Move scheduled-work implementation into `ScheduledWorkService : BaseService<...>, IScheduledWorkService`.
4. Move work-log implementation into `WorkLogService : BaseService<...>, IWorkLogService`.
5. Inject `IScheduledWorkService` and `IWorkLogService` into `TicketService`.
6. `TicketService` delegates scheduled-work and work-log `ITicketService` methods to the extracted services.
7. Do not add `IAppBLL.ScheduledWorks` or `IAppBLL.WorkLogs`.
8. Avoid circular dependencies; extracted services must not depend on `ITicketService`.

## DI plan

Register the extracted services by interface before or alongside `ITicketService`:

```csharp
services.AddScoped<IScheduledWorkService, ScheduledWorkService>();
services.AddScoped<IWorkLogService, WorkLogService>();
services.AddScoped<ITicketService, TicketService>();
```

Do not register them as AppBLL facades. Their purpose is implementation decomposition behind `TicketService`.

## WebApp MVC plan

- Update Ticket Details ViewModel/page to show transition availability and blocking reasons.
- Update AdvanceStatus POST to show blocking reasons.
- Add links from ticket details to schedule work and work logs.
- `TicketService` remains the controller-facing entry point; it may delegate scheduled-work start/complete to `IScheduledWorkService`, and lifecycle status updates should be coordinated without circular service dependencies.

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
- `IScheduledWorkService` and `IWorkLogService` BLL contracts exist.
- Scheduled-work implementation is extracted from `TicketService` into `ScheduledWorkService : BaseService<...>, IScheduledWorkService`.
- Work-log implementation is extracted from `TicketService` into `WorkLogService : BaseService<...>, IWorkLogService`.
- `TicketService` delegates scheduled-work and work-log calls to the extracted services through their interfaces.
- `IAppBLL` still exposes only `Tickets`; no `ScheduledWorks` or `WorkLogs` AppBLL properties are added.
- MVC controllers still call only `_bll.Tickets`.
- No API controller.
