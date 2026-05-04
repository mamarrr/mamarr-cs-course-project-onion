# DAL Refactor Phase 5: Introduce BLL delete orchestrator

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

### Goal

Move cross-aggregate destructive workflows out of individual repositories and into BLL-level orchestration.

Repositories should provide focused delete/query methods for their own entity. The BLL delete orchestrator coordinates the whole workflow and transaction.

### Why BLL and not DAL?

A delete workflow such as deleting a customer, property, unit, or resident is a business use case. It decides what related data must be deleted and in what order. Repositories should expose the persistence operations needed by the use case, but they should not hide the entire business workflow inside one repository.

### Proposed contracts

Add a BLL contract such as:

```text
App.BLL.Contracts/Common/Deletion/IEntityDeleteOrchestrator
```

or domain-specific contracts such as:

```text
App.BLL.Contracts/Customers/ICustomerDeleteOrchestrator
App.BLL.Contracts/Properties/IPropertyDeleteOrchestrator
App.BLL.Contracts/Units/IUnitDeleteOrchestrator
```

Use one orchestrator if the workflows are small and similar. Use domain-specific orchestrators if the workflows become large.

Recommended starting point:

```text
App.BLL.Contracts/Common/Deletion/IAppDeleteOrchestrator
App.BLL/Services/Common/Deletion/AppDeleteOrchestrator
```

### Responsibilities

The delete orchestrator should:

- Receive ids and scope ids from BLL services.
- Validate entity existence using the owning repositories.
- Start a UOW transaction when the workflow spans multiple tables.
- Ask repositories for related ids when needed.
- Call focused delete methods on the repositories that own those entities.
- Call `SaveChangesAsync` through the UOW.
- Commit or rollback transaction.
- Return a BLL-level result or bool depending on current project conventions.

The delete orchestrator should not:

- Use `AppDbContext` directly.
- Use EF Core APIs directly.
- Know SQL details.
- Be called by repositories.
- Live in `App.DAL.EF`.

### Repository methods needed by the orchestrator

Create focused repository methods such as:

```text
TicketRepository:
  AllIdsForCustomerScopeAsync(...)
  AllIdsForPropertyScopeAsync(...)
  AllIdsForUnitScopeAsync(...)
  DeleteWorkLogsForScheduledWorksAsync(...)
  DeleteScheduledWorksByTicketIdsAsync(...)
  DeleteByIdsAsync(...)

LeaseRepository:
  DeleteByUnitIdsAsync(...)
  DeleteByResidentIdAsync(...)
  DeleteByIdsAsync(...)

UnitRepository:
  AllIdsByPropertyIdsAsync(...)
  DeleteByIdsAsync(...)

PropertyRepository:
  AllIdsByCustomerIdAsync(...)
  DeleteByIdsAsync(...)

CustomerRepository:
  DeleteByIdAsync(customerId, managementCompanyId)

ResidentRepository:
  DeleteByIdAsync(residentId, managementCompanyId)

ContactRepository:
  DeleteByResidentIdAsync(...)
  DeleteByIdsAsync(...)
```

Adjust method names to match the final repository naming convention.

### First delete workflows to extract

Start with the duplicated ticket cascade because it appears in multiple delete paths:

```text
Delete tickets
  -> delete work logs for scheduled works
  -> delete scheduled works
  -> delete tickets
```

Then extract delete workflows in this order:

1. Unit delete
2. Property delete
3. Customer delete
4. Resident delete
5. Management company delete, if supported

### Example orchestration flow in plain language

Delete customer:

1. Check that the customer exists in the management company.
2. Find property ids for the customer.
3. Find unit ids for those properties.
4. Find ticket ids linked to the customer, those properties, or those units.
5. Delete ticket dependents through ticket repository methods.
6. Delete leases for units.
7. Delete customer representatives.
8. Delete units.
9. Delete properties.
10. Delete customer.
11. Save and commit.

Each delete operation should be performed by the repository that owns the entity being deleted.

### Acceptance criteria

- Large cross-aggregate delete methods are removed from repositories.
- The delete orchestrator coordinates multi-repository workflows.
- Repositories expose focused delete/query methods for their own tables.
- Transaction handling remains correct.
- Existing BLL services call the orchestrator instead of repository mega-delete methods.

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
