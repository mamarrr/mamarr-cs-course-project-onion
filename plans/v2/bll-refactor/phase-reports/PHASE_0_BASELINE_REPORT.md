# Phase 0 Baseline Report

Source brief: `plans/v2/bll-refactor/01_PHASE_0_BASELINE_AND_GUARDRAILS.md`  
Master handoff: `plans/v2/bll-refactor/00_MASTER_BLL_AGENT_HANDOFF.md`  
Date: 2026-05-06

## Build status

- Build was not run by the agent because the master handoff explicitly says: "If you want to build the solution/project, ask the user to do it manually and the user will give you build results."
- Static inspection confirms `Base.BLL.Contracts/IBaseService.cs` and `Base.BLL/BaseService.cs` currently use `FluentResults` signatures.
- Manual baseline build still needs to be provided by the project owner before Phase 1 starts.

Suggested manual command:

```powershell
dotnet build mamarrproject.sln -nologo
```

## Current IAppBLL surface

`App.BLL.Contracts/IAppBLL.cs` currently exposes 23 service properties directly:

- `AccountOnboarding`
- `OnboardingCompanyJoinRequests`
- `WorkspaceContexts`
- `WorkspaceCatalog`
- `WorkspaceRedirect`
- `ContextSelection`
- `ManagementCompanyProfiles`
- `CompanyMembershipAdmin`
- `CompanyCustomers`
- `CustomerAccess`
- `CustomerProfiles`
- `CustomerWorkspaces`
- `PropertyProfiles`
- `PropertyWorkspaces`
- `ResidentAccess`
- `ResidentProfiles`
- `ResidentWorkspaces`
- `UnitAccess`
- `UnitProfiles`
- `UnitWorkspaces`
- `LeaseAssignments`
- `LeaseLookups`
- `ManagementTickets`

This matches the wide workflow/UI-sliced surface described in the master handoff.

## Current BLL service list

Currently implemented public BLL services under `App.BLL/Services`:

- `AccountOnboardingService`
- `OnboardingCompanyJoinRequestService`
- `WorkspaceContextService`
- `UserWorkspaceCatalogService`
- `WorkspaceRedirectService`
- `ManagementCompanyProfileService`
- `CompanyMembershipAdminService`
- `CompanyCustomerService`
- `CustomerAccessService`
- `CustomerProfileService`
- `CustomerWorkspaceService`
- `PropertyProfileService`
- `PropertyWorkspaceService`
- `ResidentAccessService`
- `ResidentProfileService`
- `ResidentWorkspaceService`
- `UnitAccessService`
- `UnitProfileService`
- `UnitWorkspaceService`
- `LeaseAssignmentService`
- `LeaseLookupService`
- `ManagementTicketService`

`AppDeleteGuard` is implemented under `App.BLL/Services/Common/Deletion` and is composed by several services, but it is not exposed through `IAppBLL`.

## BaseService adoption targets

Likely aggregate-backed services or future domain services that should inherit `BaseService` once Phase 1 makes it ready:

- Customers: current `CompanyCustomerService`, `CustomerProfileService`
- Properties: current `PropertyProfileService`, possibly `PropertyWorkspaceService` only if it becomes the canonical aggregate service rather than a projection/workspace service
- Units: current `UnitProfileService`, `UnitWorkspaceService`
- Residents: current `ResidentProfileService`, `ResidentWorkspaceService`
- Leases: current `LeaseAssignmentService`
- Tickets: current `ManagementTicketService`
- Management companies: current `ManagementCompanyProfileService`

Likely orchestration or policy exceptions that should remain outside `BaseService`, or be folded behind a domain facade:

- `AccountOnboardingService`
- `OnboardingCompanyJoinRequestService`
- `WorkspaceContextService`
- `UserWorkspaceCatalogService`
- `WorkspaceRedirectService`
- `ContextSelection` implementation via `WorkspaceRedirectService`
- `CustomerAccessService`
- `ResidentAccessService`
- `UnitAccessService`
- `LeaseLookupService`
- `CompanyMembershipAdminService` if no single natural membership aggregate service is chosen
- `AppDeleteGuard`

No current `App.BLL` services inherit `BaseService`.

## Current BLL DTO shape

`App.BLL.DTO` is organized by bounded area:

- `Common`: validation model and typed app errors.
- `Contacts`: canonical `ContactBllDto`.
- `Customers`: canonical `CustomerBllDto`, command DTOs, query DTOs, projection models, customer-specific errors.
- `Leases`: canonical `LeaseBllDto`, command/query DTOs, projection and option models.
- `ManagementCompanies`: canonical `ManagementCompanyBllDto`, join-request DTO, status codes, profile and membership admin models.
- `Onboarding`: command/query DTOs and orchestration models.
- `Properties`: canonical `PropertyBllDto`, command/query DTOs, projection and option models.
- `Residents`: canonical `ResidentBllDto`, command/query DTOs, projection models, resident-specific errors.
- `Tickets`: canonical `TicketBllDto`, workflow constants, command/query DTOs, projection and option models.
- `Units`: canonical `UnitBllDto`, command/query DTOs, projection models.
- `Vendors`: canonical `VendorBllDto`.

High-level DTO categories currently present:

- Canonical aggregate DTOs: `*BllDto`.
- CRUD/page command DTOs: `Create*Command`, `Update*Command`, `Delete*Command`.
- Query DTOs carrying actor id and route slugs.
- Projection/view models for profile, workspace, dashboard, selectors, lists, and forms.
- Typed app errors based on `FluentResults.Error`.

Potential DTO simplification candidates for later phases:

- CRUD command DTOs that mostly combine `UserId`/route slugs with fields already present on canonical `*BllDto`.
- Profile update commands that duplicate canonical DTO fields and should eventually become route/scope model plus canonical DTO where no additional workflow value exists.
- Delete commands that may remain justified if they carry actor and route context, but should be reconsidered as route/scope parameters plus id.

## BaseService current behavior

Current `IBaseService<TKey, TEntity>`:

- Returns `Result<IEnumerable<TEntity>>` from `AllAsync`.
- Returns `Result<TEntity>` from `FindAsync`.
- Returns `Result<TKey>` from public `Add`.
- Returns `Result<TEntity>` from `UpdateAsync`.
- Returns `Result` from public `Remove`.
- Returns `Result` from `RemoveAsync`.

Current `BaseService` readiness observations:

- `FindAsync` returns failed `Result<TEntity>` when repository lookup returns null.
- Mapper nulls are handled as failed results in `AllAsync`, `FindAsync`, `Add`, `UpdateAsync`, and `Remove`.
- `UpdateAsync` checks repository existence before update.
- `RemoveAsync` checks repository existence before delete.
- Errors are generic string errors: `"Entity not found."` and `"Entity mapping failed."`.
- It does not reference app-specific typed errors.
- It does not authorize users, resolve route slugs, perform tenant checks, run delete guards, or contain workflow rules.

Readiness gaps against the master handoff:

- Public `Add(TEntity entity)` still exists on `IBaseService`.
- Public `Remove(TEntity entity)` still exists and can delete a mapped entity without a repository existence check.
- Protected `AddCore(entity)` does not exist yet.
- Protected `AddAndFindCoreAsync(entity, parentId, ct)` does not exist yet.
- `IBaseService` still exposes generic create, which the master handoff calls tenant/actor-unsafe for this app.

## Dependency audit

Static project-reference inspection:

- `App.BLL` references `App.BLL.Contracts`, `App.DAL.Contracts`, `App.Resources`, `Base.BLL`, `Base.BLL.Contracts`, and `Base.Contracts`.
- `App.BLL` does not reference `App.DAL.EF`.
- `App.BLL.Contracts` references `App.BLL.DTO` and `Base.BLL.Contracts`.
- `App.BLL.Contracts` does not reference `App.DAL.Contracts`, `App.DAL.DTO`, `App.DTO`, or `WebApp`.

Static text search:

- No `App.DAL.EF` usage was found in `App.BLL`, `App.BLL.Contracts`, or `App.BLL.DTO`.
- No `App.DAL.DTO` exposure was found in `App.BLL.Contracts`.
- `App.BLL` mappers and services do use `App.DAL.DTO`, which is expected under the allowed dependency rules.
- No obvious `WebApp`, MVC, `HttpContext`, `TempData`, `ViewData`, or `ViewBag` dependency was found in `App.BLL`, `App.BLL.Contracts`, or `App.BLL.DTO`.

## Known risks

- The current `IAppBLL` surface exposes access, workspace, profile, lookup, lifecycle, and onboarding services directly, which makes controller-facing service selection too granular.
- Current aggregate CRUD logic is spread across profile/workspace services rather than a small set of domain-first services.
- Current services use command/query DTOs that often carry both actor/route context and mutable entity fields. Later phases should separate untrusted route input and trusted scope from canonical entity state.
- Public `IBaseService.Add` is not tenant-safe for this application and must be removed before aggregate-backed services rely on `BaseService`.
- Public `BaseService.Remove(entity)` bypasses the existence check pattern required for tenant-scoped deletes and should not be exposed as a generic app operation.
- No current aggregate-backed BLL service inherits `BaseService`, so Phase 1 must avoid assuming downstream services are already using the base CRUD path.
- Permission checks are currently implemented inconsistently by feature-specific services and access helpers. Later grouping must preserve the existing route-slug resolution and membership/role checks before any materialization or mutation.
- `CompanyMembershipAdminService` is large and implements several contract interfaces; it needs a deliberate decision in later phases on whether membership is an aggregate-backed service or a documented orchestration exception.
- `ManagementTicketService` owns lifecycle, option loading, reference validation, delete guard usage, and profile/list projections. Later refactors must preserve ticket lifecycle guards and not flatten them into generic CRUD.

## Recommended next-phase notes

- Phase 1 should focus on `IBaseService` and `BaseService` readiness before changing app service contracts.
- Remove public generic create from `IBaseService`, replace it with protected add helpers in `BaseService`, and verify no current app code relies on public `IBaseService.Add`.
- Consider removing or restricting public `Remove(entity)` as part of BaseService readiness, because the master handoff requires delete checks to remain in concrete services and delete guards.
- Keep BaseService errors generic and non-app-specific.
- Do not move authorization, tenant resolution, delete guards, or workflow rules into BaseService.
- After BaseService is ready, introduce domain-first services incrementally around canonical DTOs while preserving the existing granular services until controller/API callers can be migrated in later approved phases.

## Assumptions

- The existing uncommitted change to `plans/v2/bll-refactor/00_MASTER_BLL_AGENT_HANDOFF.md` predates this phase report and was not modified by this agent.
- The correct report location is `plans/v2/bll-refactor/phase-reports` because the master handoff explicitly directs phase reports there.
- Build verification remains pending until the project owner runs the manual build command and provides results.
