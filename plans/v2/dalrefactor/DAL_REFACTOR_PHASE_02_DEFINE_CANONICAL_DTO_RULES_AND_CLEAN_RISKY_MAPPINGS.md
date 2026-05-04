# DAL Refactor Phase 2: Define canonical DTO rules and clean risky mappings

> Split from the original `DAL_REFACTOR_PLAN.md`.

## Shared project context

# DAL Refactor Plan

## Purpose

Refactor the DAL and related BLL orchestration so the application uses the shared `BaseRepository` and `BaseService` patterns consistently, while keeping complex business workflows explicit and understandable.

The goal is not to force every repository method into a generic base class. The goal is to make the boundary clear:

- `BaseRepository` handles simple, generic CRUD.
- Concrete repositories own entity-specific persistence and query predicates for their own aggregate/entity.
- BLL services own business validation and workflow decisions.
- Complex cross-aggregate deletes are coordinated in the BLL through a delete orchestrator, not hidden inside one repository that reaches across many DbSets.

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
  -> Delete orchestrator
      -> IAppUOW
          -> multiple repositories
              -> each repository deletes or queries only its own aggregate/entity where possible
```

## Key architectural rules

### 1. Repositories should not become use-case orchestrators

A repository should not contain a large business workflow just because one use case needs several tables.

Bad direction:

```text
CustomerRepository.DeleteAsync
  deletes work logs
  deletes scheduled works
  deletes tickets
  deletes leases
  deletes units
  deletes properties
  deletes customer representatives
  deletes customer
```

Better direction:

```text
CustomerDeleteOrchestrator in BLL
  asks repositories for related ids
  calls focused delete methods on the owning repositories
  controls transaction and workflow outcome
```

### 2. Repositories should mostly query their own DbSet

A repository may join to parent/lookup tables when needed to enforce scope or produce a projection, but it should not own unrelated entity behavior.

Acceptable examples:

- `UnitRepository` checking that a unit belongs to a property/company.
- `LeaseRepository` checking whether a lease overlaps another lease.
- `PropertyRepository` searching properties in a company.

Questionable examples to clean up:

- `LeaseRepository.ResidentExistsInCompanyAsync` querying `Residents`.
- `LeaseRepository.PropertyExistsInCompanyAsync` querying `Properties`.
- `LeaseRepository.UnitExistsInCompanyAsync` querying `Units`.
- Multiple repositories each implementing their own ticket/worklog/scheduled-work cascade deletion logic.

### 3. BLL performs validation decisions

Repositories can answer factual database questions:

- Does resident X exist in company Y?
- Does property X exist in company Y?
- Does unit X belong to property Y?
- Does an overlapping active lease exist?

BLL decides what those facts mean:

- Return validation error.
- Return not found.
- Return forbidden.
- Continue the workflow.

### 4. Use `BaseRepository` for easy CRUD only

Use inherited `BaseRepository` methods for basic operations:

- `AllAsync(ParentId)`
- `FindAsync(id, ParentId)`
- `Add(entity)`
- `Update(entity)`
- `Remove(entity)`
- `RemoveAsync(id)`

Do not put projection queries, search workflows, or destructive cascades into `BaseRepository`.

### 5. Keep one canonical DAL DTO per entity for base CRUD

Every entity that uses `BaseRepository` should have one canonical DAL DTO:

- `CustomerDalDto`
- `PropertyDalDto`
- `UnitDalDto`
- `ResidentDalDto`
- `LeaseDalDto`
- `TicketDalDto`
- etc.

This DTO is the `TDALEntity` for `BaseRepository<TDALEntity, TDomainEntity, AppDbContext>`.

Do not force all use cases to use this DTO. Keep specialized DTOs for projections and commands:

- `XxxCreateDalDto`
- `XxxUpdateDalDto`
- `XxxListItemDalDto`
- `XxxProfileDalDto`
- `XxxDashboardDalDto`
- `XxxSearchItemDalDto`

Canonical `XxxDalDto` should map safely without requiring navigation properties. If a DTO needs data from related entities, it should usually be a projection DTO instead.

## Phase plan

### Goals

- Make canonical DAL DTOs safe for `BaseRepository` mapping.
- Avoid relying on unloaded navigation properties in canonical mapper logic.

### Tasks

1. For each `XxxDalDto`, verify it contains mostly scalar properties from the same domain entity/table.
2. Remove or avoid navigation-derived fields from canonical DTOs unless the repository always loads those navigations before mapping.
3. Move navigation-derived data to projection DTOs such as profile, dashboard, list item, search item, or workspace DTOs.
4. Review all DAL mappers.
   - Mapper from domain to canonical DAL DTO must be safe if navigation properties are not loaded.
   - Mapper from canonical DAL DTO to domain entity must not accidentally wipe unrelated fields.
   - For entities with `LangStr`, avoid generic update unless the mapper/update is known to preserve translations correctly.

### Example concern to look for

If `UnitDalDto` includes `CustomerId` or `ManagementCompanyId` but `Unit` only reaches those through `Property -> Customer -> ManagementCompany`, the canonical mapper can produce empty ids when navigation properties are not loaded. Move those fields to `UnitProfileDalDto`, `UnitDashboardDalDto`, or another projection DTO.

### Acceptance criteria

- Canonical DTOs are safe for base CRUD.
- Projection DTOs carry cross-entity display data.
- No mapper depends silently on unloaded navigation properties.

## Shared execution guidance

## Commit strategy

Use small commits. Suggested sequence:

1. Stabilize base repository naming/signatures.
2. Clean canonical DTO/mappers for one simple entity.
3. Refactor one simple repository to rely on base CRUD.
4. Move lease-related existence checks to owning repositories.
5. Update lease BLL service callers.
6. Extract ticket delete helper methods into the owning repository.
7. Add BLL delete orchestrator contract and implementation.
8. Move unit delete workflow to orchestrator.
9. Move property delete workflow to orchestrator.
10. Move customer delete workflow to orchestrator.
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

## Desired final state

The final DAL should feel like this:

- Simple CRUD is inherited from `BaseRepository`.
- Canonical DAL DTOs are safe and minimal.
- Projection DTOs are used for screens/searches/workspaces.
- Repositories own their own entity queries.
- Similar predicates are not duplicated across repositories.
- Lease repository does not own resident/property/unit existence checks.
- Complex deletes are coordinated by BLL delete orchestrator using multiple repositories.
- Repositories are smaller, more focused, and easier to reason about.
