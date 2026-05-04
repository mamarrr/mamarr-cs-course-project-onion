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

## Refactor phases

## Phase 1 — Stabilize base repository usage

### Goals

- Ensure the base repository API is clear and stable.
- Avoid adding unnecessary `AsNoTracking()` because no-tracking is already configured globally.
- Ensure update methods that mutate loaded entities use `AsTracking()`.
- Avoid changing business behavior in this phase.

### Tasks

1. Review `Base.DAL.Contracts/IBaseRepository.cs`.
   - Confirm the scope parameter is consistently named `ParentId` or `parentId`.
   - Ensure method names and signatures are consistent.
   - Confirm cancellation token strategy. If adding cancellation tokens to base methods, update implementations and callers in a focused commit.

2. Review `Base.DAL.EF/BaseRepository.cs`.
   - Keep generic CRUD only.
   - Keep protected access to `RepositoryDbContext`, `RepositoryDbSet`, and mapper.
   - Do not add app-specific delete logic.
   - Do not add domain-specific joins.
   - Do not add `AsNoTracking()` unless intentionally overriding global behavior.

3. Audit current concrete repositories.
   - Mark which methods are generic CRUD and can use inherited base methods.
   - Mark which methods are projections/searches and should remain custom.
   - Mark which methods are cross-aggregate workflows and should move to BLL orchestration.

### Acceptance criteria

- Base repository still builds.
- No business workflow has been moved yet.
- A clear method classification exists for each repository.

## Phase 2 — Define canonical DTO rules and clean risky mappings

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

## Phase 3 — Repository method ownership cleanup

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

## Phase 4 — Use `BaseRepository` for simple CRUD in concrete repositories

### Goals

- Remove duplicated simple CRUD from concrete repositories.
- Keep custom methods for projections, scoped reads, searches, and updates involving `LangStr` or complex field rules.

### Tasks per repository

For each repository:

1. Confirm it inherits from `BaseRepository<XxxDalDto, XxxDomainEntity, AppDbContext>`.
2. Confirm `IXxxRepository` inherits from `IBaseRepository<XxxDalDto>`.
3. Remove custom implementations that duplicate base `AllAsync`, `FindAsync`, `Add`, `Update`, or `RemoveAsync`, if behavior is identical.
4. Keep custom `AddAsync` methods when creation uses a separate create DTO or sets defaults such as generated ids, slugs, timestamps, active flags, or `LangStr` fields.
5. Keep custom `UpdateAsync` methods when update rules differ from simple full entity replacement.
6. Keep custom projection methods.
7. Keep custom query predicates that are owned by this repository.

### Recommended order

Start with simpler repositories:

1. `ContactRepository`
2. `ManagementCompanyJoinRequestRepository`
3. `LookupRepository`
4. `UnitRepository`
5. `PropertyRepository`
6. `ResidentRepository`
7. `CustomerRepository`
8. `LeaseRepository`
9. `TicketRepository`

Do not start with `CustomerRepository`, `PropertyRepository`, `UnitRepository`, or `TicketRepository` delete workflows.

### Acceptance criteria

- Easy CRUD uses `BaseRepository` where possible.
- Concrete repositories contain only meaningful custom behavior.
- Build succeeds after each repository or small group of repositories.

## Phase 5 — Introduce BLL delete orchestrator

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

## Phase 6 — BLL service cleanup after DAL refactor

### Goals

- Update BLL services to use the cleaned repository boundaries.
- Keep validation decisions in BLL.
- Prepare for later use of `BaseService` where appropriate.

### Tasks

1. Update services that currently call old repository methods.
2. Move validation flow to BLL where missing.
3. Use the correct repository for each factual predicate.
4. Keep app-specific service methods that return `FluentResults` and validation errors.
5. Do not force domain-specific services into `BaseService` if they are workflow-heavy.

### BaseService usage guidance

Use `BaseService` only for simple CRUD-style BLL services.

Do not force these into `BaseService` yet:

- onboarding services
- lease assignment workflows
- ticket workflows
- profile services with access checks
- membership administration
- delete orchestration

Potential future simple candidates:

- lookup-like services
- simple admin CRUD screens
- small aggregate services without complex validation/access logic

## Phase 7 — Final repository audit

### Checklist per repository

For every repository, verify:

- It inherits from `BaseRepository` where applicable.
- It uses the canonical DAL DTO for base CRUD.
- It does not duplicate base CRUD without reason.
- It does not query unrelated DbSets except for necessary parent-scope checks or projections.
- It does not orchestrate large cross-aggregate workflows.
- It exposes existence predicates only for its own entity.
- It uses clear method names.
- It uses `AsTracking()` for load-and-mutate update methods.
- It relies on global no-tracking for read methods unless explicit override is needed.
- It does not contain duplicated logic that exists in another repository.

### Checklist for BLL

- Validation decisions are in BLL services.
- Cross-aggregate deletes are in a BLL delete orchestrator.
- BLL uses repositories through `IAppUOW`.
- BLL does not use EF Core or `AppDbContext` directly.
- BLL does not depend on WebApp or ASP.NET Identity infrastructure.

### Checklist for contracts

- Repository contracts expose persistence/query capabilities, not business workflows.
- BLL contracts expose business use cases.
- DTO namespaces are consistent with project boundaries.
- No obsolete methods remain in contracts.

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
