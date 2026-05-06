# Phase 2 BaseService Decisions

Source briefs:

- `plans/v2/bll-refactor/00_MASTER_BLL_AGENT_HANDOFF.md`
- `plans/v2/bll-refactor/03_PHASE_2_BASESERVICE_RULES.md`
- `plans/v2/bll-refactor/phase-reports/PHASE_0_BASELINE_REPORT.md`
- `plans/v2/bll-refactor/phase-reports/PHASE_1_BLL_INVENTORY.md`

## Build Status

Not run by this agent. The master handoff explicitly instructs agents not to build projects or the solution.

Required user verification:

```powershell
dotnet build mamarrproject.sln -nologo
```

Report `OK` or provide the build errors before Phase 3 starts.

## Scope Executed

Updated only the BaseService contract and implementation plus this decision report:

- `Base.BLL.Contracts/IBaseService.cs`
- `Base.BLL/BaseService.cs`
- `plans/v2/bll-refactor/phase-reports/PHASE_2_BASESERVICE_DECISIONS.md`

No repositories, UOW architecture, WebApp/API controllers, DTOs, tests, DAL EF, migrations, or app workflow services were changed.

## Required Decisions

| Decision | Outcome | Rationale |
| --- | --- | --- |
| Should `BaseService.FindAsync` return `Result<T>` instead of `Result<T?>` when not-found is failure? | Yes. `IBaseService.FindAsync` now returns `Task<Result<TEntity>>`; `BaseService.FindAsync` returns `Task<Result<TBLLEntity>>`. | A failed `Result` represents not-found, so successful values should be non-null. This keeps future aggregate services clearer and avoids double-signaling with both failure and nullable payloads. |
| Should mapper null checks replace null-forgiving `!` in BaseService? | Yes. | `IBaseMapper` returns nullable values by contract. BaseService now fails with a generic mapping error when a mapper unexpectedly returns null. |
| Should `Remove(entity)` remain in `IBaseService`, or should app services prefer `RemoveAsync(id, parentId)`? | Keep `Remove(entity)` for Base compatibility, but app domain services should prefer `RemoveAsync(id, parentId)`. | Entity-based remove is useful for already-loaded entities, but tenant-scoped app services need id plus parent scope so they can authorize and constrain before deletion. |
| Are generic BaseService string errors acceptable? | Yes, in Base only. | Base projects must not depend on app-specific errors such as `NotFoundError`. |
| Should app services wrap/override BaseService generic errors with typed app errors where needed? | Yes. | Public app services should expose typed app errors for expected outcomes, especially not-found, forbidden, validation, conflict, and business-rule results. |
| Which exposed services are aggregate-backed and must inherit BaseService? | See aggregate-backed target list below. | These have a natural aggregate, canonical BLL DTO, DAL DTO, repository, and mapper path. |
| Which exposed services are orchestration exceptions? | See orchestration exception list below. | These are workflow, context, access, policy, or projection services without natural CRUD ownership. |

## BaseService Behavior Rules

- `AllAsync(parentId)` returns `Result<IEnumerable<T>>`; mapping failure returns a failed `Result`.
- `FindAsync(id, parentId)` returns `Result<T>`; missing repository rows return failed `Result` with `Entity not found.`.
- `Add(entity)` maps BLL to DAL and returns the repository-created id; mapping failure returns failed `Result<TKey>`.
- `UpdateAsync(entity, parentId)` checks existence before update, maps both input and repository result, and returns failed `Result<T>` on not-found or mapping failure.
- `Remove(entity)` remains available, but future app services should not use it for tenant-scoped public delete paths unless authorization and materialization already happened safely.
- `RemoveAsync(id, parentId)` remains the preferred app-service delete primitive because it carries the parent scope.
- BaseService errors stay generic strings. Concrete App.BLL services may wrap inherited results or override methods to return typed app errors.
- BaseService does not enforce tenant membership, role checks, lifecycle rules, cross-tenant FK validation, or workflow policy. Concrete App.BLL services must enforce those before exposing aggregate operations.

## Aggregate-Backed Service Targets

Future public domain services exposed from `IAppBLL` should inherit `BaseService` with their primary aggregate:

| Future service | Primary aggregate | Canonical BLL DTO | Repository | Mapper |
| --- | --- | --- | --- | --- |
| `CustomerService` | Customer | `CustomerBllDto` | `ICustomerRepository` | `CustomerBllDtoMapper` |
| `PropertyService` | Property | `PropertyBllDto` | `IPropertyRepository` | `PropertyBllDtoMapper` |
| `UnitService` | Unit | `UnitBllDto` | `IUnitRepository` | `UnitBllDtoMapper` |
| `ResidentService` | Resident | `ResidentBllDto` | `IResidentRepository` | `ResidentBllDtoMapper` |
| `LeaseService` | Lease | `LeaseBllDto` | `ILeaseRepository` | `LeaseBllDtoMapper` |
| `TicketService` | Ticket | `TicketBllDto` | `ITicketRepository` | `TicketBllDtoMapper` |
| `ManagementCompanyService` | Management company | `ManagementCompanyBllDto` | `IManagementCompanyRepository` | `ManagementCompanyBllDtoMapper` |

Conditional aggregate-backed targets:

| Future service | Status |
| --- | --- |
| `ContactService` | BaseService-ready if exposed later. |
| `VendorService` | BaseService-ready if exposed later. |
| `OnboardingService` join request support | May compose a join-request aggregate service, but the public onboarding facade should remain an orchestration exception unless later phases explicitly split it. |
| `CompanyMembershipService` | Not BaseService-backed today because no natural canonical membership BLL DTO/repository path was identified in the prior inventory. |

## Orchestration Exceptions

These services may be exposed from the final domain-first facade without inheriting `BaseService`, provided their exception status remains documented:

| Future service | Reason |
| --- | --- |
| `OnboardingService` | Multi-step account, management company, join request, and onboarding-state workflow. |
| `WorkspaceService` | Cross-domain workspace catalog, remembered-context redirect, and context-selection authorization. |
| `CompanyMembershipService` | Membership authorization, role assignment, owner transfer, pending access review, and capability policy; no natural BaseService aggregate path today. |

These should become internal helpers/policies rather than public domain CRUD services:

- `CustomerAccessService`
- `ResidentAccessService`
- `UnitAccessService`
- `AppDeleteGuard`
- `SlugGenerator`
- Internal validators, builders, and small pure policy helpers

## Repository and Dependency Notes

- Repositories must continue returning plain entities or nullable entities, not `FluentResults`.
- `BaseService` remains the BLL boundary that converts repository null outcomes to failed `Result` outcomes.
- No App-specific typed errors were introduced into Base projects.
- No app workflow logic was moved into BaseService.

## Risks / TODOs

- Build status is unknown until the user runs the required build.
- Future services must not expose inherited `FindAsync(id)` or `RemoveAsync(id)` without tenant-aware parent scope and authorization wrappers.
- App services that surface BaseService outcomes directly should translate generic Base errors into typed app errors where callers depend on error classification.
- Later phases should avoid treating client-provided tenant ids as authority when replacing command DTOs with canonical DTO plus explicit scope parameters.

## Handoff to Phase 3

Final BaseService method signatures:

```csharp
Task<Result<IEnumerable<TEntity>>> AllAsync(TKey parentId = default!, CancellationToken cancellationToken = default);
Task<Result<TEntity>> FindAsync(TKey id, TKey parentId = default!, CancellationToken cancellationToken = default);
Result<TKey> Add(TEntity entity);
Task<Result<TEntity>> UpdateAsync(TEntity entity, TKey parentId = default!, CancellationToken cancellationToken = default);
Result Remove(TEntity entity);
Task<Result> RemoveAsync(TKey id, TKey parentId = default!, CancellationToken cancellationToken = default);
```

Approved usage rule:

```text
Every aggregate-backed public domain BLL service inherits BaseService using its natural primary aggregate.
Pure orchestration services may be documented exceptions.
BaseService provides CRUD mechanics only; concrete services enforce tenant, role, lifecycle, and workflow policy.
```

Recommended commit message after build verification is OK:

```text
bll: stabilize BaseService result contracts
```

## Assumptions

- The user's request to implement Phase 2 counts as approval for the small BaseService cleanup recommended by the phase brief.
- Static inspection is sufficient for this phase because the master handoff prohibits agents from running the build.
- No current `App.BLL` service inherits `BaseService`, so changing `FindAsync` from `Result<T?>` to `Result<T>` should not require app-service call-site updates in this phase.
