# DAL/BLL Refactor Phase 5 — BLL Delete Guard and Generic Blocked-Delete Policy

> Scope file only. Use this together with `DAL_REFACTOR_MASTER_GUIDANCE.md`.
> Do not implement other phases unless explicitly instructed.

## Goal

Replace cross-aggregate cascade delete workflows with blocked-delete validation.

Deletion should be blocked when any dependent record exists. The UI does **not** need dependency counts and does **not** need specific dependency names.

The main Phase 5 rule:

```text
Delete only the requested entity.
Do not automatically delete related business entities.
Block deletion when any dependency exists.
Return a generic business error.
```

Generic error message target:

```text
Unable to delete because dependent records exist.
```

or, if slightly more user-friendly:

```text
Unable to delete this item because it is still referenced by other records.
```

Do not spend Phase 5 building detailed dependency messages, dependency counts, or UI dependency breakdowns.

---

# Why blocked delete?

Blocked delete is safer and simpler than orchestrated cascade delete.

Instead of:

```text
Delete customer
  -> delete tickets
  -> delete scheduled works
  -> delete work logs
  -> delete leases
  -> delete units
  -> delete properties
  -> delete representatives
  -> delete customer
```

Use:

```text
Delete customer
  -> resolve access/scope
  -> check permission
  -> check whether any dependency exists
  -> if dependency exists, return generic BusinessRuleError
  -> if no dependency exists, delete only customer
```

---

# Current project context

Phase 4 standardized repository CRUD around `BaseRepository`.

Therefore Phase 5 should use this delete target:

```text
After the delete guard allows deletion:
  prefer BaseRepository.RemoveAsync(id, parentId, cancellationToken)
```

Do not continue using old custom cascade `DeleteAsync(...)` methods after a workflow has been converted.

The current BLL has split profile/service entry points. Phase 5 should convert these current delete flows:

```text
UnitProfileService.DeleteAsync
PropertyProfileService.DeleteAsync
CustomerProfileService.DeleteAsync
ResidentProfileService.DeleteAsync
ManagementTicketService.DeleteAsync
```

Later optional flows:

```text
ManagementCompanyProfileService.DeleteAsync, if management company deletion is supported
Vendor delete, if vendor deletion exists
Contact delete, if contacts become independently deletable
Lease delete, if lease deletion gains blockers
```

---

# BLL delete guard placement

Add a BLL delete guard as an internal BLL helper.

Recommended files:

```text
App.BLL.Contracts/Common/Deletion/IAppDeleteGuard.cs
App.BLL/Services/Common/Deletion/AppDeleteGuard.cs
```

Do **not** create count-based dependency DTOs for Phase 5.

No need for:

```text
DeleteDependencyModel
DeleteDependencySummaryModel
CustomerDeleteDependencySummaryModel
PropertyDeleteDependencySummaryModel
UnitDeleteDependencySummaryModel
ResidentDeleteDependencySummaryModel
TicketDeleteDependencySummaryModel
```

unless a later UI requirement explicitly needs detailed dependency reporting.

For this phase, boolean dependency checks are enough.

## AppBLL wiring

Do not expose `IAppDeleteGuard` on `IAppBLL` unless WebApp needs to call it directly.

Preferred approach:

```text
AppBLL privately creates AppDeleteGuard.
AppBLL passes IAppDeleteGuard into services that perform deletes.
```

Example services that should receive the guard:

```text
UnitProfileService
PropertyProfileService
CustomerProfileService
ResidentProfileService
ManagementTicketService
```

The delete guard is a BLL helper, not a user-facing application service.

---

# Delete guard responsibilities

The delete guard should:

- receive ids and scope ids from BLL services;
- ask owning repositories whether any delete-blocking dependency exists;
- return allowed/blocked;
- use `IAppUOW`;
- avoid direct EF Core and `AppDbContext` usage.

The delete guard should not:

- delete anything;
- call `SaveChangesAsync`;
- use `AppDbContext` directly;
- use EF Core APIs directly;
- know SQL details;
- be called by repositories;
- live in `App.DAL.EF`;
- return dependency counts;
- return dependency names for UI display;
- build detailed dependency reports.

Recommended guard method style:

```text
CanDeleteUnitAsync(unitId, propertyId, managementCompanyId, cancellationToken)
CanDeleteTicketAsync(ticketId, managementCompanyId, cancellationToken)
CanDeletePropertyAsync(propertyId, customerId, managementCompanyId, cancellationToken)
CanDeleteCustomerAsync(customerId, managementCompanyId, cancellationToken)
CanDeleteResidentAsync(residentId, managementCompanyId, cancellationToken)
```

Each guard method can return either:

```text
Task<bool>
```

where `true` means delete is allowed, or a small internal result type such as:

```text
DeleteGuardResult.Allowed
DeleteGuardResult.Blocked
```

Do not overbuild this. A boolean is enough unless the project style benefits from a small result object.

---

# DAL dependency predicate responsibilities

Repositories should expose focused boolean dependency predicate methods owned by the repository for the entity being deleted.

Examples:

```text
IUnitRepository:
  HasDeleteDependenciesAsync(unitId, propertyId, managementCompanyId, cancellationToken)

IPropertyRepository:
  HasDeleteDependenciesAsync(propertyId, customerId, managementCompanyId, cancellationToken)

ICustomerRepository:
  HasDeleteDependenciesAsync(customerId, managementCompanyId, cancellationToken)

IResidentRepository:
  HasDeleteDependenciesAsync(residentId, managementCompanyId, cancellationToken)

ITicketRepository:
  HasDeleteDependenciesAsync(ticketId, managementCompanyId, cancellationToken)
```

These methods should return:

```text
Task<bool>
```

Meaning:

```text
true  -> dependency exists, deletion must be blocked
false -> no known dependency exists, deletion may proceed
```

Repository predicate methods should use efficient `AnyAsync(...)` queries.

Do not use `CountAsync(...)` unless there is a real reason. Counting is unnecessary when the UI only needs a generic blocked-delete error.

The repository method should not:

- delete anything;
- return full child entities;
- perform BLL authorization;
- format UI messages;
- return dependency counts;
- return BLL DTOs.

---

# BLL delete flow

Each converted delete method should follow this flow:

```text
1. Resolve access/scope.
2. Confirm entity exists.
3. Validate delete confirmation input.
4. Check permissions.
5. Ask IAppDeleteGuard whether deletion is allowed.
6. If blocked, return generic BusinessRuleError.
7. If allowed, call repository RemoveAsync(id, parentId, cancellationToken).
8. Save changes.
9. Return success.
```

Use the existing project result style:

```text
FluentResults
BusinessRuleError
NotFoundError
ForbiddenError
ValidationAppError
```

Blocked delete should normally return `BusinessRuleError`.

Recommended blocked-delete message:

```text
Unable to delete because dependent records exist.
```

or:

```text
Unable to delete this item because it is still referenced by other records.
```

Do not return dependency-specific messages such as:

```text
Cannot delete this unit because it has 3 leases and 2 tickets.
```

Do not return counts to the UI.

---

# Transaction guidance

Old cascade deletes often used explicit transactions because one user action deleted many related rows.

After conversion to blocked delete, most delete flows should be:

```text
dependency predicate check
RemoveAsync one entity
SaveChangesAsync
```

So:

```text
Remove explicit transaction blocks from converted delete workflows
unless the converted workflow still performs multiple writes.
```

Do not keep transactions merely because the old cascade method used one.

---

# Database FK safety net

The database should still prevent invalid deletes through FK constraints.

If an FK violation still occurs:

- catch it at a suitable boundary if the project already has exception handling patterns;
- return the same generic blocked-delete business error;
- do not expose raw FK constraint names to users.

Do not rely only on FK exceptions for normal UX. Prefer explicit `HasDeleteDependenciesAsync(...)` checks through the delete guard.

---

# Conversion order

Implement Phase 5 in vertical slices.

## Phase 5A — first blocked-delete workflows

1. Unit delete
2. Ticket delete

Reason:

```text
Unit delete currently has a clear cascade workflow.
Ticket delete currently removes scheduled work/work logs.
Both are good first examples for replacing cascade delete with blocked delete.
```

## Phase 5B — core profile deletes

3. Property delete
4. Customer delete
5. Resident delete

## Phase 5C — optional/later workflows

6. Vendor delete, if vendor delete exists
7. Management company delete, if supported
8. Contact delete, if contacts become independently deletable
9. Lease delete, if lease deletion gains independent blockers

---

# Vertical slice template

For each converted delete workflow:

1. Add repository contract method: `HasDeleteDependenciesAsync(...)`.
2. Implement repository dependency predicate using `AnyAsync(...)`.
3. Add or update delete guard method.
4. Inject/pass `IAppDeleteGuard` into the BLL service.
5. Replace old cascade delete call with guard + base `RemoveAsync`.
6. Return generic `BusinessRuleError` if the guard blocks deletion.
7. Remove old custom cascade `DeleteAsync(...)` from repository contract and implementation if no longer needed.
8. Remove transaction block if the delete now removes only one entity.
9. Build and test the converted workflow.
10. Commit/checkpoint before the next slice.

No dependency count DTOs are required.

---

# Unit delete target slice

Current old behavior should be replaced:

```text
UnitProfileService.DeleteAsync
  -> _uow.Units.DeleteAsync(unitId, propertyId, managementCompanyId)
  -> UnitRepository.DeleteAsync cascades tickets, scheduled work, work logs, leases, unit
```

New behavior:

```text
UnitProfileService.DeleteAsync
  -> resolve unit workspace
  -> confirm unit number
  -> check role
  -> delete guard checks whether any unit dependency exists
  -> if dependency exists, return generic BusinessRuleError
  -> if allowed, _uow.Units.RemoveAsync(unitId, propertyId, cancellationToken)
  -> SaveChangesAsync
```

Unit dependency predicate should check whether any of these exist:

```text
leases
tickets
```

Implementation should use `AnyAsync(...)` and short-circuit where practical.

If scheduled work/work logs are only reachable through tickets, they do not need to be checked separately for unit deletion.

After conversion:

```text
Remove IUnitRepository.DeleteAsync if it is no longer used.
Remove UnitRepository.DeleteAsync if it is no longer used.
```

---

# Ticket delete target slice

Current old behavior:

```text
ManagementTicketService.DeleteAsync
  -> _uow.Tickets.DeleteAsync(ticketId, managementCompanyId)
  -> TicketRepository.DeleteAsync deletes scheduled work, work logs, ticket
```

New behavior:

```text
ManagementTicketService.DeleteAsync
  -> resolve company access
  -> delete guard checks whether any ticket dependency exists
  -> if dependency exists, return generic BusinessRuleError
  -> if allowed, _uow.Tickets.RemoveAsync(ticketId, managementCompanyId, cancellationToken)
  -> SaveChangesAsync
```

Ticket dependency predicate should check whether any of these exist:

```text
scheduled work
work logs through scheduled work
```

Do not delete scheduled work or work logs automatically.

After conversion:

```text
Remove ITicketRepository.DeleteAsync if it is no longer used.
Remove TicketRepository.DeleteAsync if it is no longer used.
```

---

# Property delete target slice

Current old behavior should be replaced:

```text
PropertyProfileService.DeleteAsync
  -> _uow.Properties.DeleteAsync(propertyId, customerId, managementCompanyId)
```

New behavior:

```text
PropertyProfileService.DeleteAsync
  -> resolve property workspace
  -> confirm property name
  -> check role
  -> delete guard checks whether any property dependency exists
  -> if dependency exists, return generic BusinessRuleError
  -> if allowed, _uow.Properties.RemoveAsync(propertyId, customerId, cancellationToken)
  -> SaveChangesAsync
```

Property dependency predicate should check whether any of these exist:

```text
units
leases
tickets
```

---

# Customer delete target slice

Current old behavior should be replaced:

```text
CustomerProfileService.DeleteAsync
  -> _uow.Customers.DeleteAsync(customerId, managementCompanyId)
```

New behavior:

```text
CustomerProfileService.DeleteAsync
  -> resolve customer access
  -> confirm customer name
  -> check role
  -> delete guard checks whether any customer dependency exists
  -> if dependency exists, return generic BusinessRuleError
  -> if allowed, _uow.Customers.RemoveAsync(customerId, managementCompanyId, cancellationToken)
  -> SaveChangesAsync
```

Customer dependency predicate should check whether any of these exist:

```text
properties
units
leases
tickets
customer representatives
```

---

# Resident delete target slice

Current old behavior should be replaced:

```text
ResidentProfileService.DeleteAsync
  -> _uow.Residents.DeleteAsync(residentId, managementCompanyId)
```

New behavior:

```text
ResidentProfileService.DeleteAsync
  -> resolve resident workspace
  -> confirm resident id code
  -> check role
  -> delete guard checks whether any resident dependency exists
  -> if dependency exists, return generic BusinessRuleError
  -> if allowed, _uow.Residents.RemoveAsync(residentId, managementCompanyId, cancellationToken)
  -> SaveChangesAsync
```

Resident dependency predicate should check whether any of these exist:

```text
leases
tickets
resident users
resident contacts
customer representative links
```

---

# Out of scope

Do not implement cascade delete orchestration.

Do not delete related business entities automatically.

Do not rewrite all BLL services at once.

Do not change database schema unless FK behavior is clearly wrong.

Do not create count-based dependency DTOs.

Do not return dependency counts to UI.

Do not return dependency-specific UI messages unless a later requirement asks for that.

Do not introduce a new result/error pattern unrelated to the current `FluentResults` style.

Do not expose raw FK constraint names to users.

---

# Acceptance criteria

Phase 5 is complete when:

- `IAppDeleteGuard` and `AppDeleteGuard` exist.
- Delete guard checks dependencies through repositories and `IAppUOW`.
- Boolean dependency predicate methods exist on owning repositories for converted workflows.
- Converted delete methods return generic `BusinessRuleError` when any dependency exists.
- Converted delete methods delete only the requested entity when allowed.
- Converted delete methods use base `RemoveAsync` where possible.
- Converted repository cascade `DeleteAsync` methods are removed once no longer needed.
- Explicit transaction blocks are removed from simple converted deletes.
- No dependency counts are returned to UI.
- Build succeeds.
