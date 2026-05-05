# DAL Refactor Phase 3 — Remaining Subplans

This file supplements Phase 3 of the DAL refactor.

Use it together with:

- `DAL_REFACTOR_MASTER_GUIDANCE.md`
- `DAL_REFACTOR_PHASE_03_REPOSITORY_METHOD_OWNERSHIP_CLEANUP.md`
- `DAL_REFACTOR_PHASE_01_REPOSITORY_METHOD_CLASSIFICATION.md`, if available

## Purpose

The first Phase 3 pass moved the most explicit `LeaseRepository` existence checks to the owning repositories. This file defines the remaining Phase 3 cleanup work:

1. Move lease-assignment search/list methods out of `LeaseRepository` to the repositories that own the queried data.
2. Split `TicketRepository` option/reference methods by owning repository.
3. Update and preserve the Phase 5 delete-dependency predicate list for the blocked-delete phase.

Do not implement Phase 5 delete guard behavior in this phase. Phase 3 may update the dependency-predicate list and may add small repository predicate methods if they are part of ownership cleanup, but blocked-delete business behavior belongs to Phase 5.

---

# Subplan 3A — Move lease-assignment search/list methods

## Current issue

`LeaseRepository` no longer owns the simple existence checks for units, residents, properties, and lease roles. However, it still owns lease-assignment helper queries that primarily query other DbSets:

- property search for lease assignment
- unit list for selected property
- resident search for lease assignment
- lease role list

These methods support the lease-assignment UI/use case, but their primary queried entities are not `Lease`.

## Decision

These methods should move out of `LeaseRepository`.

Reason:

```text
Repositories should own the data they primarily query.
BLL services should own the use case that combines those queries.
```

## Required moves

| Current responsibility | Current owner | Target owner | Suggested target method name |
|---|---|---|---|
| Search properties for lease assignment | `LeaseRepository` | `PropertyRepository` | `SearchForLeaseAssignmentAsync` |
| List units for selected property | `LeaseRepository` | `UnitRepository` | `ListForLeaseAssignmentAsync` |
| Search residents for lease assignment | `LeaseRepository` | `ResidentRepository` | `SearchForLeaseAssignmentAsync` |
| List lease roles | `LeaseRepository` | `LookupRepository` | `ListLeaseRolesAsync` |

## BLL coordination target

After the move, the lease lookup/use-case service should coordinate the calls through `IAppUOW`.

Target shape:

```text
LeaseLookupService or LeaseAssignmentService
  -> UOW.Properties.SearchForLeaseAssignmentAsync(...)
  -> UOW.Units.ListForLeaseAssignmentAsync(...)
  -> UOW.Residents.SearchForLeaseAssignmentAsync(...)
  -> UOW.Lookups.ListLeaseRolesAsync(...)
```

## Implementation notes

- Keep the existing DTOs if they are still useful for the lease-assignment UI.
- It is acceptable for the DTOs to remain under `App.DAL.DTO.Leases` if they are lease-assignment-specific read models.
- If desired later, move DTOs to the owning entity folders, but this is not required for this subplan.
- Update repository contracts, EF implementations, `IAppUOW`, and BLL callers together.
- Remove the moved methods from `ILeaseRepository` and `LeaseRepository` after callers are updated.

## Acceptance criteria

- `ILeaseRepository` no longer exposes property search, unit list, resident search, or lease role list methods.
- `LeaseRepository` no longer queries `Properties`, `Units`, `Residents`, or `LeaseRoles` for lease-assignment selector data.
- `PropertyRepository` owns the property search query.
- `UnitRepository` owns the unit list query.
- `ResidentRepository` owns the resident search query.
- `LookupRepository` owns the lease role list query.
- The BLL lease lookup/use-case service still returns the same functional selector data.
- Solution builds.

---

# Subplan 3B — Split TicketRepository option/reference methods by owning repository

## Current issue

`TicketRepository` currently owns too many responsibilities. It handles ticket persistence and ticket projections, but it also acts as:

- ticket lookup provider
- customer option provider
- property option provider
- unit option provider
- resident option provider
- vendor option provider
- cross-aggregate reference validator

This violates the Phase 3 rule that repositories should not reach too far outside their natural DbSet.

## Keep in `TicketRepository`

Keep methods that are truly ticket-owned:

- ticket list by company/filter
- ticket details projection
- ticket edit projection
- next ticket number generation
- ticket number uniqueness check
- ticket add
- ticket update
- ticket status update
- ticket delete, temporarily until Phase 5 blocked-delete work

## Move ticket lookup methods to `LookupRepository`

Move ticket lookup/option methods away from `TicketRepository`.

Target owner: `LookupRepository`.

Move or replace:

- `FindStatusByCodeAsync`
- `FindStatusByIdAsync`
- `AllStatusesAsync`
- `AllPrioritiesAsync`
- `AllCategoriesAsync`

Suggested target names:

- `FindTicketStatusByCodeAsync`
- `FindTicketStatusByIdAsync`
- `AllTicketStatusesAsync`
- `AllTicketPrioritiesAsync`
- `AllTicketCategoriesAsync`
- `TicketCategoryExistsAsync`
- `TicketPriorityExistsAsync`
- `TicketStatusExistsAsync`

The exact names may differ, but they should clearly identify the lookup type.

## Move ticket selector option methods to owning repositories

Move selector option methods according to the entity being listed:

| Current method | Target repository | Suggested target method name |
|---|---|---|
| `CustomerOptionsAsync` | `CustomerRepository` | `TicketOptionsAsync` or `OptionsForTicketAsync` |
| `PropertyOptionsAsync` | `PropertyRepository` | `TicketOptionsAsync` or `OptionsForTicketAsync` |
| `UnitOptionsAsync` | `UnitRepository` | `TicketOptionsAsync` or `OptionsForTicketAsync` |
| `ResidentOptionsAsync` | `ResidentRepository` | `TicketOptionsAsync` or `OptionsForTicketAsync` |
| `VendorOptionsAsync` | vendor owner, if vendors exist | `TicketOptionsAsync`, or remove if vendors are deleted |

If vendors were removed from the domain/repository layer, remove or disable vendor-specific ticket option and validation paths rather than leaving dead repository methods.

## Move `ValidateReferencesAsync` out of `TicketRepository`

`ValidateReferencesAsync` should not stay in `TicketRepository`.

Reason:

- It validates category, priority, status, customer, property, unit, resident, and vendor relationships.
- That is cross-aggregate validation.
- Repositories should answer factual questions.
- BLL should decide which validation errors to return.

This belongs to Phase 3, not Phase 5, because it is repository ownership cleanup, not delete-policy work.

## Target validation shape

Keep the BLL validation method in `ManagementTicketService`, or extract it into a BLL helper/service if it becomes too large.

The BLL should call focused repository predicates such as:

- lookup repository: category exists
- lookup repository: priority exists
- lookup repository: status exists
- customer repository: customer exists in company
- property repository: property exists in company
- property repository: property belongs to customer
- unit repository: unit exists in company
- unit repository: unit belongs to property
- resident repository: resident exists in company
- resident repository: resident is linked to unit, if that relationship is required
- vendor repository: vendor exists in company, only if vendors still exist

The BLL should then construct the validation failures and return the existing BLL result/error style.

## Suggested predicate methods

Add or reuse focused predicates where needed:

### `ICustomerRepository`

- `ExistsInCompanyAsync(customerId, managementCompanyId)`

### `IPropertyRepository`

- `ExistsInCompanyAsync(propertyId, managementCompanyId)`
- `ExistsInCustomerAsync(propertyId, customerId)`

### `IUnitRepository`

- `ExistsInCompanyAsync(unitId, managementCompanyId)`
- `ExistsInPropertyAsync(unitId, propertyId)`

### `IResidentRepository`

- `ExistsInCompanyAsync(residentId, managementCompanyId)`
- `IsLinkedToUnitAsync(residentId, unitId)` if required by current ticket rules

### `ILookupRepository`

- `TicketCategoryExistsAsync(categoryId)`
- `TicketPriorityExistsAsync(priorityId)`
- `TicketStatusExistsAsync(statusId)`
- `FindTicketStatusByCodeAsync(code)`
- `FindTicketStatusByIdAsync(statusId)`

## Update BLL callers

Update `ManagementTicketService`:

- `BuildOptionsAsync` should no longer call ticket repository for customer/property/unit/resident/lookup options.
- status lookup calls should use `LookupRepository`.
- reference validation should call owning repositories directly instead of `_uow.Tickets.ValidateReferencesAsync(...)`.
- remove the DAL DTO used only for old aggregate reference validation if it becomes unused.

## Acceptance criteria

- `ITicketRepository` no longer exposes lookup option methods.
- `ITicketRepository` no longer exposes customer/property/unit/resident/vendor option methods.
- `ITicketRepository` no longer exposes `ValidateReferencesAsync`.
- `TicketRepository` no longer queries unrelated tables just to build selector options or validate cross-aggregate references.
- BLL ticket service still validates the same business rules.
- BLL ticket service still builds selector options.
- Solution builds.

---

# Subplan 3C — Update Phase 5 delete-dependency predicate list

## Current status

A Phase 5 delete-dependency predicate list already exists in the refactor planning material. This subplan updates and clarifies it so the Phase 5 agent can implement blocked-delete behavior without rediscovering the dependency graph.

Phase 3 should not implement the delete guard. It should ensure the Phase 5 predicate list is accurate after repository ownership cleanup.

## Blocked-delete policy reminder

The delete strategy is blocked delete, not cascade delete.

For important business entities:

```text
1. BLL checks dependencies.
2. If dependencies exist, return a clear business error.
3. If no dependencies exist, delete only the requested entity.
4. Database FK constraints remain as a safety net.
```

## Updated dependency predicate list for Phase 5

### Management company delete blockers

A management company should not be deleted if it has any important child records.

Likely blockers:

- active or historical memberships
- join requests
- customers
- properties under customers
- units under properties
- residents
- leases
- tickets
- ticket scheduled work / work logs
- vendors, if vendors exist

Suggested Phase 5 repository predicates:

- `ManagementCompanyRepository.GetDeleteDependencySummaryAsync(managementCompanyId)`
- or smaller predicates on owning repositories:
  - `CustomerRepository.CountByCompanyAsync(managementCompanyId)`
  - `ResidentRepository.CountByCompanyAsync(managementCompanyId)`
  - `TicketRepository.CountByCompanyAsync(managementCompanyId)`

### Customer delete blockers

A customer should not be deleted if it has business records below it.

Likely blockers:

- properties
- units through properties
- leases through units
- tickets linked to customer/property/unit
- customer representatives
- contacts linked only through customer representative relationships, depending on contact ownership policy

Suggested Phase 5 predicates:

- `CustomerRepository.GetDeleteDependencySummaryAsync(customerId, managementCompanyId)`
- `PropertyRepository.CountByCustomerAsync(customerId)`
- `UnitRepository.CountByCustomerAsync(customerId)`
- `LeaseRepository.CountByCustomerAsync(customerId)` or `CountByUnitIdsAsync(...)`
- `TicketRepository.CountByCustomerScopeAsync(customerId, managementCompanyId)`
- `CustomerRepository.CountRepresentativesAsync(customerId)`

### Property delete blockers

A property should not be deleted if it has dependent business records.

Likely blockers:

- units
- leases through units
- tickets linked to property or units

Suggested Phase 5 predicates:

- `PropertyRepository.GetDeleteDependencySummaryAsync(propertyId, customerId)`
- `UnitRepository.CountByPropertyAsync(propertyId)`
- `LeaseRepository.CountByPropertyAsync(propertyId)` or `CountByUnitIdsAsync(...)`
- `TicketRepository.CountByPropertyScopeAsync(propertyId, managementCompanyId)`

### Unit delete blockers

A unit should not be deleted if it has dependent business records.

Likely blockers:

- leases
- tickets
- scheduled work/work logs through tickets

Suggested Phase 5 predicates:

- `UnitRepository.GetDeleteDependencySummaryAsync(unitId, propertyId, managementCompanyId)`
- `LeaseRepository.CountByUnitAsync(unitId)`
- `TicketRepository.CountByUnitAsync(unitId, managementCompanyId)`

### Resident delete blockers

A resident should not be deleted if it has dependent business records.

Likely blockers:

- leases
- tickets
- customer representative assignments
- resident user links
- resident contacts, depending on contact ownership policy

Suggested Phase 5 predicates:

- `ResidentRepository.GetDeleteDependencySummaryAsync(residentId, managementCompanyId)`
- `LeaseRepository.CountByResidentAsync(residentId, managementCompanyId)`
- `TicketRepository.CountByResidentAsync(residentId, managementCompanyId)`
- `ResidentRepository.CountRepresentativeAssignmentsAsync(residentId)`
- `ResidentRepository.CountUserLinksAsync(residentId)`
- `ContactRepository.CountByResidentAsync(residentId)` if contacts block deletion

### Ticket delete blockers

A ticket should not be deleted if it has workflow/history records that should be preserved.

Likely blockers:

- scheduled works
- work logs through scheduled works

Suggested Phase 5 predicates:

- `TicketRepository.GetDeleteDependencySummaryAsync(ticketId, managementCompanyId)`
- `TicketRepository.CountScheduledWorksAsync(ticketId)`
- `TicketRepository.CountWorkLogsAsync(ticketId)`

### Lease delete blockers

Lease delete may be simpler than other deletes, but still define the policy explicitly.

Likely blockers:

- none, if leases can be deleted directly
- or block delete if lease has future workflow/history records later

Suggested Phase 5 predicates:

- `LeaseRepository.GetDeleteDependencySummaryAsync(leaseId, managementCompanyId)` if needed
- otherwise document that lease delete has no dependency blockers currently

### Contact delete blockers

Contact delete depends on the ownership policy.

Possible policy A:

- contacts are owned child data and can be removed if unlinked

Possible policy B:

- contacts are business records and deletion is blocked while linked

Suggested Phase 5 predicates:

- `ContactRepository.CountResidentLinksAsync(contactId)`
- `ContactRepository.CanDeleteAsync(contactId)`

## Phase 5 delete guard target

The Phase 5 agent should use this list to implement a BLL-level delete guard, for example:

```text
IAppDeleteGuard
  CanDeleteCustomerAsync(...)
  CanDeletePropertyAsync(...)
  CanDeleteUnitAsync(...)
  CanDeleteResidentAsync(...)
  CanDeleteTicketAsync(...)
```

The delete guard should:

- call the owning repositories for dependency summaries/counts
- return dependency details
- not delete anything
- not use EF Core directly
- not use `AppDbContext` directly

## Phase 3 acceptance criteria for this subplan

- The Phase 5 delete-dependency predicate list is updated in planning material.
- The list reflects the blocked-delete strategy.
- The list names likely blockers for each major business entity.
- The list does not require implementing delete guard behavior during Phase 3.
- Existing cascade delete methods may remain until Phase 5.

---

# Recommended implementation order

1. Move lease-assignment selector methods out of `LeaseRepository`.
2. Move ticket lookup methods to `LookupRepository`.
3. Move ticket selector option methods to owning repositories.
4. Refactor ticket BLL reference validation to call owning repositories directly.
5. Remove obsolete option/reference methods from `ITicketRepository` and `TicketRepository`.
6. Update the Phase 5 dependency-predicate list if implementation reveals missing blockers.
7. Build the solution.

# Final Phase 3 completion criteria

Phase 3 can be considered complete when:

- lease existence checks are already moved
- lease-assignment selector methods are moved to owning repositories
- ticket lookup methods are moved to `LookupRepository`
- ticket selector option methods are moved to owning repositories
- ticket reference validation no longer lives in `TicketRepository`
- Phase 5 delete-dependency predicate list is updated
- solution builds
