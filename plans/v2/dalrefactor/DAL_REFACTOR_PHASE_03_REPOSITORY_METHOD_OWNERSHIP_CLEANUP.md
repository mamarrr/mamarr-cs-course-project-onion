# DAL Refactor Phase 3: Repository method ownership cleanup

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

- Repositories should not reach too far outside their natural DbSet.
- Simple existence checks should live in the repository that owns the checked entity.
- Similar semantic methods should be consolidated.

### General rule

Move methods according to the entity they primarily query:

```text
Resident existence/checks  -> ResidentRepository
Property existence/checks  -> PropertyRepository
Unit existence/checks      -> UnitRepository
Lease existence/checks     -> LeaseRepository
Ticket existence/checks    -> TicketRepository
Lookup/role checks         -> LookupRepository
```

### Lease repository cleanup

Move these methods out of `ILeaseRepository` / `LeaseRepository`:

- `ResidentExistsInCompanyAsync` -> `IResidentRepository` / `ResidentRepository`
- `PropertyExistsInCompanyAsync` -> `IPropertyRepository` / `PropertyRepository`
- `UnitExistsInCompanyAsync` -> `IUnitRepository` / `UnitRepository`
- `LeaseRoleExistsAsync` -> `ILookupRepository` / `LookupRepository`, if lease roles are lookup data

Keep this in `ILeaseRepository` / `LeaseRepository`:

- `HasOverlappingActiveLeaseAsync`
- lease list/detail methods
- lease create/update/delete methods that operate on `Leases`

Review these later; they may be moved after first cleanup:

- `SearchPropertiesAsync` may belong in `PropertyRepository` as a lease-assignment search query.
- `SearchResidentsAsync` may belong in `ResidentRepository` as a lease-assignment search query.
- `ListUnitsForPropertyAsync` may belong in `UnitRepository`.
- `ListLeaseRolesAsync` may belong in `LookupRepository`.

### Similar semantics audit

Search all repositories for methods with similar names or behavior:

- `ExistsInCompanyAsync`
- `ExistsInCustomerAsync`
- `SlugExists...`
- `RegistryCodeExists...`
- `AllSlugs...`
- `FindActive...Context...`
- `Find...RoleCode...`
- `Search...`
- `List...Options...`
- `Delete...`

For each duplicate semantic pattern:

1. Keep the method in the repository that owns the queried entity.
2. Standardize naming where possible.
3. Avoid two repositories implementing the same predicate over the same table.
4. Update BLL services to call the new repository location through `IAppUOW`.
5. Remove old methods from old repository contracts and implementations.

### Suggested naming conventions

Use simple, consistent names:

```text
ExistsInCompanyAsync(entityId, managementCompanyId)
ExistsInCustomerAsync(entityId, customerId)
ExistsInPropertyAsync(entityId, propertyId)
SlugExistsInCompanyAsync(...)
SlugExistsInCustomerAsync(...)
AllSlugsByParentAsync(...)
SearchForLeaseAssignmentAsync(...)
ListForLeaseAssignmentAsync(...)
```

Avoid names that include the caller use case unless the query is truly use-case-specific.

### Acceptance criteria

- `LeaseRepository` no longer owns resident/property/unit existence predicates.
- Each moved method is exposed on the correct repository contract.
- BLL services compile against the new method locations.
- No duplicate method with the same semantics remains in multiple repositories unless there is a clear reason.

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
