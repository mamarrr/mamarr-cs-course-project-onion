# DAL Refactor Master Guidance — Blocked Delete Strategy

Use this master guidance file together with exactly one phase scope file.

The phase file tells the agent what to do now.
This master file tells the agent how to think, what boundaries to respect, and what not to break.

## How to use this file

For every implementation pass, give the AI agent:

1. This file: `DAL_REFACTOR_MASTER_GUIDANCE.md`
2. One phase file: `DAL_REFACTOR_PHASE_XX_...md`

The agent should implement only the selected phase. It should not start work from later phases unless explicitly instructed.

## Purpose

Refactor the DAL and related BLL validation so the application uses the shared `BaseRepository` and `BaseService` patterns consistently, while keeping business workflows explicit and safe.

The goal is not to force every repository method into a generic base class. The goal is to make the boundary clear:

- `BaseRepository` handles simple, generic CRUD.
- Concrete repositories own entity-specific persistence and query predicates for their own aggregate/entity.
- BLL services own business validation and workflow decisions.
- Deletes of important business entities should be blocked when dependent records exist.
- Database FK constraints should act as a final safety net, not as the primary user-facing error mechanism.

## Current assumptions

- `IBaseRepository` already uses `ParentId` instead of the old misleading `appUserId` naming.
- `AppDbContext` is globally configured with `QueryTrackingBehavior.NoTrackingWithIdentityResolution`, so repository read queries do not need explicit `AsNoTracking()` unless a method intentionally wants to override the global default.
- Methods that load an entity and mutate it must still use `AsTracking()` explicitly.
- Existing repositories already inherit from `BaseRepository` in many places, but many methods still duplicate CRUD logic or reach into unrelated DbSets.
- The refactor should be incremental. Do not attempt to rewrite every repository in one commit.

## Target architecture

```text
WebApp
  -> IAppBLL
      -> BLL services
          -> IAppUOW
              -> focused repositories
                  -> AppDbContext / DbSet owned by the repository
```

For delete workflows:

```text
BLL service
  -> BLL delete guard / delete policy
      -> IAppUOW
          -> focused repositories
              -> dependency checks and simple deletes
```

## Phase index

1. Stabilize base repository usage
2. Define canonical DTO rules and clean risky mappings
3. Repository method ownership cleanup
4. Use BaseRepository for simple CRUD in concrete repositories
5. Introduce BLL delete guard and blocked-delete policy
6. BLL service cleanup after DAL refactor
7. Final repository audit

## Global execution rules

- Work incrementally.
- Keep commits small and focused.
- Do not mix CRUD cleanup, repository ownership cleanup, DTO cleanup, and delete policy changes in the same change unless the active phase explicitly requires it.
- Preserve current behavior unless the active phase explicitly changes it.
- Run a solution build after each phase or after a small group of related edits.
- Prefer moving code to the correct layer over rewriting business behavior.
- Update contracts, implementations, and callers together.
- Remove obsolete methods only after all callers have been moved.
- Do not hide business workflows in generic base infrastructure.

## Global architecture rules

### Repositories

Repositories should:

- Own persistence and query predicates for their own aggregate/entity.
- Use `BaseRepository` for simple CRUD.
- Keep projection/search methods only when they are naturally owned by that repository.
- Expose dependency-check methods for their own entity where needed for delete blocking.
- Use `AsTracking()` for load-and-mutate update methods.
- Rely on global no-tracking behavior for read methods unless an explicit override is needed.
- Avoid querying unrelated DbSets except for parent-scope checks or projection joins that are truly needed.

Repositories should not:

- Become use-case orchestrators.
- Contain large cross-aggregate cascade delete workflows.
- Delete unrelated aggregates as part of deleting one entity.
- Duplicate predicates that belong in another repository.
- Reach into many unrelated DbSets just because a caller needs a workflow.

### BLL services

BLL services should:

- Own validation decisions.
- Coordinate use cases.
- Use repositories through `IAppUOW`.
- Call delete guards before attempting destructive operations.
- Return BLL-level results/errors according to current project conventions.

BLL services should not:

- Use EF Core directly.
- Use `AppDbContext` directly.
- Depend on WebApp.
- Depend on ASP.NET Identity infrastructure.
- Cascade-delete large business object graphs.

### BaseRepository

`BaseRepository` should:

- Stay generic.
- Handle simple CRUD only.
- Use canonical DAL DTOs.
- Support simple parent/scope filtering where the base infrastructure already supports it.

`BaseRepository` should not:

- Know app-specific workflows.
- Delete dependent aggregates.
- Contain projection/search logic.
- Contain domain-specific joins.
- Catch database FK exceptions and turn them into business messages.

### DTOs

Canonical DAL DTOs should:

- Be safe for base CRUD.
- Implement the relevant base entity interface.
- Mostly contain scalar fields from the same table/entity.
- Avoid data that requires unloaded navigation properties.

Projection DTOs should:

- Carry cross-entity display data.
- Be used for profile, dashboard, list, search, workspace, and UI-specific DAL projections.

### Delete policy

The default delete policy is **blocked delete**, not cascade delete.

For important business entities, do not delete a whole graph automatically. Instead:

1. BLL checks whether the entity may be deleted.
2. The delete guard asks repositories for dependency summaries/counts.
3. If dependencies exist, BLL returns a clear business error.
4. If no dependencies exist, BLL calls the owning repository to delete only the requested entity.
5. Database FK constraints remain as a safety net if an unexpected dependency exists.

Good blocked-delete examples:

```text
Cannot delete customer because it has:
- 2 properties
- 14 units
- 6 leases
- 3 tickets
Remove or reassign these records before deleting the customer.
```

Use blocked delete for:

- management companies
- customers
- properties
- units
- residents
- leases
- tickets
- other business-important records

Use automatic cascade only for true owned data with no independent business meaning, if the domain model clearly supports it.

## Layering rules

Correct direction:

```text
WebApp
  -> IAppBLL
      -> BLL services
          -> IAppUOW
              -> repositories
                  -> AppDbContext
```

Delete workflow direction:

```text
BLL service
  -> BLL delete guard / delete policy
      -> IAppUOW
          -> focused repository dependency checks
  -> owning repository simple delete, if allowed
```

Incorrect direction:

```text
Repository
  -> BLL service

BaseRepository
  -> app-specific delete workflow

BLL service
  -> AppDbContext

Repository
  -> unrelated repository through service locator

Repository
  -> delete full cross-aggregate graph
```

## Repository ownership naming guidance

Use simple names where possible:

```text
ExistsInCompanyAsync(entityId, managementCompanyId)
ExistsInCustomerAsync(entityId, customerId)
ExistsInPropertyAsync(entityId, propertyId)
HasDeleteDependenciesAsync(...)
GetDeleteDependencySummaryAsync(...)
SlugExistsInCompanyAsync(...)
SlugExistsInCustomerAsync(...)
AllSlugsByParentAsync(...)
SearchForLeaseAssignmentAsync(...)
ListForLeaseAssignmentAsync(...)
```

Move predicates according to the entity they primarily query:

```text
Resident existence/checks       -> ResidentRepository
Resident dependency checks      -> ResidentRepository
Property existence/checks       -> PropertyRepository
Property dependency checks      -> PropertyRepository
Unit existence/checks           -> UnitRepository
Unit dependency checks          -> UnitRepository
Lease existence/checks          -> LeaseRepository
Lease dependency checks         -> LeaseRepository
Ticket existence/checks         -> TicketRepository
Ticket dependency checks        -> TicketRepository
Lookup/role checks              -> LookupRepository
```

## Build and verification expectations

For each phase:

- Build the solution.
- Fix compile errors caused by moved contracts or methods.
- Check for stale methods in repository contracts.
- Check for stale using statements.
- Check that BLL callers use the new repository locations.
- Avoid leaving duplicate methods with the same semantics in multiple repositories unless documented.
- Verify delete behavior returns useful business errors instead of silently deleting dependent graphs.

## Commit strategy

Use small commits. Suggested sequence:

1. Stabilize base repository naming/signatures.
2. Clean canonical DTO/mappers for one simple entity.
3. Refactor one simple repository to rely on base CRUD.
4. Move lease-related existence checks to owning repositories.
5. Update lease BLL service callers.
6. Add dependency summary DTOs/contracts for blocked delete.
7. Add BLL delete guard contract and implementation.
8. Replace unit cascade delete with blocked-delete validation.
9. Replace property cascade delete with blocked-delete validation.
10. Replace customer cascade delete with blocked-delete validation.
11. Final repository duplicate-method audit.

After every commit or small group of commits, run the solution build.

## Non-goals for this refactor

Do not rewrite the whole BLL architecture.
Do not rewrite all controllers.
Do not change database schema unless the refactor reveals a real model problem.
Do not convert every service to `BaseService`.
Do not remove projection DTOs just to use one DTO everywhere.
Do not hide destructive business workflows inside `BaseRepository`.
Do not add `AsNoTracking()` everywhere because the DbContext default already handles no-tracking reads.
Do not implement broad cascade delete orchestration for business-important entities.
Do not rely only on raw database FK exception text for user-facing messages.

## Desired final state

The final DAL/BLL delete model should feel like this:

- Simple CRUD is inherited from `BaseRepository`.
- Canonical DAL DTOs are safe and minimal.
- Projection DTOs are used for screens/searches/workspaces.
- Repositories own their own entity queries.
- Similar predicates are not duplicated across repositories.
- Lease repository does not own resident/property/unit existence checks.
- Complex business deletes are blocked when dependencies exist.
- BLL delete guard returns dependency summaries and clear business errors.
- Repositories delete only their own entity or true owned child rows.
- Database FK constraints protect against missed dependencies.
- Repositories are smaller, more focused, and easier to reason about.
