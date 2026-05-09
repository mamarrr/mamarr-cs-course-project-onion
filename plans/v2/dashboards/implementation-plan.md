# Dashboard Implementation Plan

## Summary

Implement Portal dashboards as a read-only, API-neutral dashboard/query feature. The dashboard layer composes operational read models across company, portfolio, resident, ticket, lease, and scheduled-work data without becoming a domain owner.

This fits the current Onion architecture and can later move cleanly into a modular monolith as a `PortalDashboards` or `OperationsDashboard` read module.

The implementation pass covers MVC controllers, Razor views, ViewModels, BLL contracts/models/services, DAL contracts/DTOs/repository projections, and mappers. Public REST API controllers are out of scope, but the BLL models must remain reusable by future API controllers without MVC dependencies.

## Architectural Direction

Use a dedicated dashboard service and a dedicated dashboard repository, with clear read-model boundaries:

- `IPortalDashboardService` belongs in `App.BLL.Contracts/Dashboards`.
- `PortalDashboardService` belongs in `App.BLL/Services/Dashboards`.
- `IPortalDashboardRepository` belongs in `App.DAL.Contracts/Repositories/Dashboards`.
- `PortalDashboardRepository` belongs in `App.DAL.EF/Repositories/Dashboards`.
- DAL dashboard DTOs belong in `App.DAL.DTO/Dashboards`.
- BLL dashboard models belong in `App.BLL.DTO/Dashboards/Models`.
- BLL mappers for DAL DTO -> BLL model belong in `App.BLL/Mappers/Dashboards`.
- MVC ViewModels and WebApp-only BLL model -> ViewModel mapping belong in `WebApp`.

The dashboard service and repository should not inherit from the CRUD base abstractions:

- Do not inherit `PortalDashboardRepository` from `BaseRepository`.
- Do not inherit `PortalDashboardService` from `BaseService`.
- `BaseRepository` and `BaseService` are for entity-owned CRUD workflows with `All`, `Find`, `Add`, `Update`, and `Remove` semantics.
- Dashboards are read-only, cross-aggregate query models with counts, breakdowns, previews, and timelines; there is no single dashboard domain entity to persist.

This is acceptable within the solution architecture. Architectural consistency here means preserving the direction and boundaries:

`MVC -> IAppBLL -> BLL service -> IAppUOW -> DAL repository -> EF projections`

It does not mean every service/repository must inherit from the CRUD base classes. `AdminDashboardRepository` and `AdminDashboardService` already provide a precedent for dashboard-style read services that do not use `BaseRepository`/`BaseService`.

Treat `PortalDashboardRepository` strictly as a read/query repository:

- It performs scoped aggregate queries, groupings, and preview-list projections.
- It does not own lifecycle rules, permissions, tenant decisions, validation rules, or writes.
- It never returns domain entities or EF entities.
- It returns dashboard-specific DAL DTOs only.

Treat `PortalDashboardService` as the dashboard application boundary:

- It resolves workspace and authorization through `IPortalContextProvider`.
- It owns dashboard definitions such as open, overdue, high-priority, delayed, top-list size, and date windows.
- It passes resolved IDs and policy inputs into DAL, not raw route values as authority.
- It returns `FluentResults` with API-neutral BLL models.

This avoids coupling dashboards to MVC while still keeping the feature pragmatic in the current project structure. In a future modular monolith, this code should become an operations/read module that consumes module-level query interfaces or read projections, not a module that owns customer/property/ticket/resident business rules.

Use this rule for future consistency:

> `BaseRepository`/`BaseService` are expected for entity-owned CRUD services. Read-model/query services may use dedicated interfaces and implementations, but must still follow DTO boundaries, UOW access, BLL contracts, mappers where useful, and tenant-safe authorization.

## Contracts And Registration

Add `IPortalDashboardService` with these methods:

- `Task<Result<ManagementDashboardModel>> GetManagementDashboardAsync(ManagementCompanyRoute route, CancellationToken cancellationToken = default);`
- `Task<Result<CustomerDashboardModel>> GetCustomerDashboardAsync(CustomerRoute route, CancellationToken cancellationToken = default);`
- `Task<Result<PropertyDashboardModel>> GetPropertyDashboardAsync(PropertyRoute route, CancellationToken cancellationToken = default);`
- `Task<Result<UnitDashboardModel>> GetUnitDashboardAsync(UnitRoute route, CancellationToken cancellationToken = default);`
- `Task<Result<ResidentDashboardModel>> GetResidentDashboardAsync(ResidentRoute route, CancellationToken cancellationToken = default);`

Expose the service through `IAppBLL.PortalDashboards` and lazy-create it in `AppBLL`.

Add `IPortalDashboardRepository` with matching read methods that accept resolved IDs and a small query-options DTO. Suggested shape:

- `PortalDashboardQueryOptionsDalDto`
  - `DateTime UtcNow`
  - `DateTime TodayStartUtc`
  - `DateTime TomorrowStartUtc`
  - `DateTime NextSevenDaysEndUtc`
  - `DateTime RecentSinceUtc`
  - `int PreviewLimit`
  - `IReadOnlySet<string> OpenTicketExcludedStatusCodes`
  - `IReadOnlySet<string> HighPriorityCodes`
  - `IReadOnlySet<string> CompletedOrCancelledWorkStatusCodes`

Expose the repository through `IAppUOW.PortalDashboards` and instantiate it in `AppUOW`.

Register `IPortalDashboardService` in `AddAppBll`. No separate repository DI registration is needed because repositories are exposed through `IAppUOW`.

Existing placeholder dashboard methods on `CustomerService`, `PropertyService`, `ResidentService`, and `UnitService` may remain for compatibility, but MVC dashboard controllers should use `_bll.PortalDashboards`.

## Dashboard Models

Create dashboard-specific BLL models. Do not reuse MVC ViewModels, EF entities, or broad domain DTOs as dashboard contracts.

Dashboard DTO/model types do not need to implement `IBaseEntity`. They are read models, not persisted entities. Avoid adding fake IDs just to satisfy base CRUD abstractions.

Use shared model concepts where possible:

- `DashboardMetricModel`
  - `string Key`
  - `string Label`
  - `int Value`
  - optional route/link metadata if needed
- `DashboardBreakdownItemModel`
  - `string Code`
  - `string Label`
  - `int Count`
- `DashboardTicketPreviewModel`
  - ticket ID, ticket number, title, status code/label, priority label, optional due date, created date, and route-safe context slugs
- `DashboardWorkPreviewModel`
  - scheduled work ID, ticket ID/number/title, vendor name, work status code/label, scheduled start/end, real start/end
- `DashboardRecentActivityModel`
  - item type, label, supporting text, created/event date, route-safe link target data
- `DashboardContextLinkModel`
  - label, route kind, and route values needed by MVC/API consumers

Each full dashboard model should include its resolved workspace/context model plus purpose-specific sections:

- Management dashboard: company context, summary metrics, ticket command metrics, work command metrics, join request metrics/list, team metrics, recent activity.
- Customer dashboard: customer context, compact profile, portfolio metrics, ticket metrics/breakdowns/list, representative summary/list, recent activity.
- Resident dashboard: resident context, compact profile, active leases, ticket metrics/list, contact summary, representations.
- Property dashboard: property context, compact profile, unit metrics/breakdowns/list preview, ticket metrics/breakdowns/list, residents/leases, scheduled work.
- Unit dashboard: unit context, compact profile, occupancy summary, ticket metrics/breakdowns/list, scheduled work, timeline.

If names collide with existing placeholder models such as `App.BLL.DTO.Properties.Models.PropertyDashboardModel`, either replace those placeholder models with the new shape or move the new models under `App.BLL.DTO.Dashboards.Models` with unambiguous names such as `PortalPropertyDashboardModel`.

## Mapper Guidance

Use mapper classes for clarity, but do not force a bidirectional CRUD-style mapper if it obscures the read-only intent.

Acceptable options:

- Preferred: dedicated dashboard mapper classes with explicit read-only methods, for example `MapManagementDashboard(...)`, `MapTicketPreview(...)`, and `MapMetric(...)`.
- Also acceptable: implement `IBaseMapper<TBllModel, TDalDto>` only where it stays clean. If used, the reverse `BLL -> DAL` direction should clearly throw `NotSupportedException` or be avoided by convention, because dashboard models are not written back.

Do not create domain-to-DAL mappers for dashboards, because dashboards do not have corresponding domain entities.

## Query Rules

Apply these rules consistently in BLL and pass their code sets/time windows into DAL:

- Open tickets: ticket status code is not `CLOSED`.
- Overdue tickets: `DueAt < UtcNow` and status code is not `CLOSED`.
- High-priority tickets: priority code is `HIGH` or `URGENT`.
- Delayed work: planned start or planned end is before `UtcNow` and work status code is not `DONE` or `CANCELLED`.
- Recent activity: created/event date is at or after `UtcNow.AddDays(-30)`.
- Today: `ScheduledStart >= TodayStartUtc && ScheduledStart < TomorrowStartUtc`.
- This week: next 7 days from today, `ScheduledStart >= TodayStartUtc && ScheduledStart < NextSevenDaysEndUtc`.
- Preview lists: top 5 unless a future requirement changes it.
- Resident linked user accounts and login access status remain excluded.

Use seeded stable codes for ticket statuses, ticket priorities, work statuses, and join request statuses. Do not depend on localized labels for filtering.

## DAL Query Requirements

All DAL methods must be tenant-scoped by resolved IDs:

- Management dashboard: `ManagementCompanyId`.
- Customer dashboard: `ManagementCompanyId` + `CustomerId`.
- Property dashboard: `ManagementCompanyId` + `CustomerId` + `PropertyId`.
- Unit dashboard: `ManagementCompanyId` + `PropertyId` + `UnitId`.
- Resident dashboard: `ManagementCompanyId` + `ResidentId`.

Queries should use `AsNoTracking()` and project directly into DAL DTOs.

Avoid loading large lists and counting in memory. Use database-side counts, grouped counts, and top-N projections.

Route data in DAL DTOs must be sufficient for MVC links, but must not leak cross-tenant existence. Any row included in a preview list must be reached through the same tenant/workspace scope as the parent dashboard.

Use `LangStr.ToString()` in projections for localized persisted domain values, following the existing repository pattern.

## BLL Mapping And Authorization

Each `PortalDashboardService` method should:

1. Validate/resolve the route through `IPortalContextProvider`.
2. Return `UnauthorizedError`, `ForbiddenError`, or `NotFoundError` consistently with existing Portal services.
3. Build dashboard query options once using `DateTime.UtcNow`.
4. Call the dashboard repository with resolved IDs.
5. Map DAL DTOs into API-neutral BLL models.
6. Return `Result.Ok(model)`.

The BLL service must not expose DAL DTOs to MVC. MVC must not call `IAppUOW` or repository methods directly.

Keep business policy out of DAL. If a definition changes later, such as high-priority including only `URGENT`, that change should happen in BLL query-option construction, not in repository business logic.

## MVC Implementation

Update these controllers to call `_bll.PortalDashboards`:

- `WebApp/Areas/Portal/Controllers/Management/DashboardController.cs`
- `WebApp/Areas/Portal/Controllers/Customer/CustomerDashboardController.cs`
- `WebApp/Areas/Portal/Controllers/Property/DashboardController.cs`
- `WebApp/Areas/Portal/Controllers/Unit/DashboardController.cs`
- `WebApp/Areas/Portal/Controllers/Resident/DashboardController.cs`

Keep AppChrome construction in controllers as it is today. Build page titles and active sections through existing navigation conventions.

Create/expand strongly typed dashboard ViewModels in `WebApp/ViewModels` and map from BLL dashboard models into them. This mapper is WebApp-specific and may include route names, CSS/status badge hints, and display formatting helpers.

Replace placeholder dashboard Razor content with real dashboard sections:

- Compact context card at the top.
- Metric card rows.
- Command/attention cards for overdue, high-priority, delayed, and due-soon items.
- Top-5 preview lists with links to the appropriate details or section pages.
- Empty states when a section has no rows.

Use existing Portal UI classes:

- `management-grid`
- `management-grid-2`
- `management-grid-3`
- `management-card`
- `management-kpi`
- `management-badge`
- `management-table`
- `management-muted`
- `management-btn`

Add small shared dashboard partials only where duplication is obvious, such as metric cards, preview-list rows, and empty states. Avoid a generic dashboard renderer that hides each dashboard's intent.

## Localization

Add English and Estonian resource keys together for all static dashboard UI labels:

- dashboard section headings
- metric labels
- empty-state messages
- generic labels such as `Overdue`, `HighPriority`, `DueThisWeek`, `RecentActivity`, `ScheduledToday`, `DelayedWork`

Persisted labels such as status, priority, category, property type, work status, lease role, contact type, and representative role must come from existing `LangStr`-backed database labels.

Do not hard-code user-visible English strings in Razor or controllers except temporary fallback values following existing local patterns.

## Verification

Run a build after implementation:

- Preferred: `dotnet build App.Domain\App.Domain.csproj -nologo` if the current approved quality gate remains narrow.
- Broader: build the solution or WebApp project if dashboard changes require it and the environment allows it.

Do not add tests while the project testing override is active.

Manual review checklist:

- MVC controllers depend only on `IAppBLL`, not `IAppUOW`, repositories, DAL DTOs, or EF.
- BLL models do not reference MVC/ViewModel types.
- DAL repositories do not return domain entities or EF entities.
- All dashboard queries are scoped by resolved tenant/workspace IDs.
- No query fetches by ID alone for tenant-scoped data.
- Resident linked user accounts/login access are not displayed.
- All static UI text has English and Estonian resource entries.

## Assumptions

- Dashboard implementation is read-only.
- No schema or migration changes are required.
- API controllers are future work, but BLL models must be reusable without MVC dependencies.
- Preview lists use top 5.
- Recent activity means last 30 days.
- This week means the next 7 days from today, not the calendar week.
