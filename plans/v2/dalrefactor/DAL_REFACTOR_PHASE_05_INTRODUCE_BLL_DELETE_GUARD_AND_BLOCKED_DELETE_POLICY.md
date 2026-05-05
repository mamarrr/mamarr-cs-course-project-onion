# DAL/BLL Refactor Phase 5 — BLL Delete Guard and Blocked-Delete Policy

> Scope file only. Use this together with `DAL_REFACTOR_MASTER_GUIDANCE.md`.
> Do not implement other phases unless explicitly instructed.

## Goal

Replace cross-aggregate cascade delete workflows with blocked-delete validation.

For important business entities, deletion should be blocked when dependent records exist. The user should receive a clear business error explaining why the delete cannot happen.

The main Phase 5 rule:

```text
Delete only the requested entity.
Do not automatically delete related business entities.
Block deletion when dependent records exist.
```

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
  -> check dependencies
  -> if dependencies exist, return BusinessRuleError
  -> if no dependencies exist, delete only customer
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

Recommended dependency models:

```text
App.BLL.DTO/Common/Deletion/DeleteDependencyModel.cs
App.BLL.DTO/Common/Deletion/DeleteDependencySummaryModel.cs
```

or, if clearer, domain-specific BLL models:

```text
CustomerDeleteDependencySummaryModel
PropertyDeleteDependencySummaryModel
UnitDeleteDependencySummaryModel
ResidentDeleteDependencySummaryModel
TicketDeleteDependencySummaryModel
```

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
- ask owning repositories for dependency summaries/counts;
- return whether deletion is allowed;
- return dependency information for business errors;
- use `IAppUOW`;
- avoid direct EF Core and `AppDbContext` usage.

The delete guard should not:

- delete anything;
- call `SaveChangesAsync`;
- use `AppDbContext` directly;
- use EF Core APIs directly;
- know SQL details;
- be called by repositories;
- live in `App.DAL.EF`.

---

# DAL dependency summary responsibilities

Repositories should expose focused dependency summary methods owned by the repository for the entity being deleted.

Examples:

```text
IUnitRepository:
  GetDeleteDependencySummaryAsync(unitId, propertyId, managementCompanyId, cancellationToken)

IPropertyRepository:
  GetDeleteDependencySummaryAsync(propertyId, customerId, managementCompanyId, cancellationToken)

ICustomerRepository:
  GetDeleteDependencySummaryAsync(customerId, managementCompanyId, cancellationToken)

IResidentRepository:
  GetDeleteDependencySummaryAsync(residentId, managementCompanyId, cancellationToken)

ITicketRepository:
  GetDeleteDependencySummaryAsync(ticketId, managementCompanyId, cancellationToken)
```

Repository dependency summary DTOs should live in DAL DTOs, not BLL DTOs.

Recommended location:

```text
App.DAL.DTO/Common/Deletion
```

or domain-specific files such as:

```text
App.DAL.DTO/Units/UnitDeleteDependencySummaryDalDto.cs
App.DAL.DTO/Properties/PropertyDeleteDependencySummaryDalDto.cs
App.DAL.DTO/Customers/CustomerDeleteDependencySummaryDalDto.cs
App.DAL.DTO/Residents/ResidentDeleteDependencySummaryDalDto.cs
App.DAL.DTO/Tickets/TicketDeleteDependencySummaryDalDto.cs
```

BLL may then map/compose these DAL summaries into BLL errors or BLL dependency models.

---

# BLL delete flow

Each converted delete method should follow this flow:

```text
1. Resolve access/scope.
2. Confirm entity exists.
3. Validate delete confirmation input.
4. Check permissions.
5. Ask IAppDeleteGuard whether deletion is allowed.
6. If blocked, return BusinessRuleError with dependency details.
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

Example user-facing message style:

```text
Cannot delete this unit because it still has leases and tickets linked to it.
```

If returning dependency details, keep them structured enough for UI later, but do not overbuild UI formatting in this phase.

---

# Transaction guidance

Old cascade deletes often used explicit transactions because one user action deleted many related rows.

After conversion to blocked delete, most delete flows should be:

```text
dependency check
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
- return a generic blocked-delete business error;
- do not expose raw FK constraint names to users.

Do not rely only on FK exceptions for normal UX. Prefer explicit dependency checks through the delete guard.

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

1. Add DAL dependency summary DTO.
2. Add repository contract method: `GetDeleteDependencySummaryAsync(...)`.
3. Implement repository dependency summary with counts/flags only.
4. Add or update delete guard method.
5. Inject/pass `IAppDeleteGuard` into the BLL service.
6. Replace old cascade delete call with guard + base `RemoveAsync`.
7. Remove old custom cascade `DeleteAsync(...)` from repository contract and implementation if no longer needed.
8. Remove transaction block if the delete now removes only one entity.
9. Build and test the converted workflow.
10. Commit/checkpoint before the next slice.

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
  -> delete guard checks unit dependencies
  -> if dependencies exist, return BusinessRuleError
  -> if allowed, _uow.Units.RemoveAsync(unitId, propertyId, cancellationToken)
  -> SaveChangesAsync
```

Unit dependencies should include at minimum:

```text
LeaseCount
TicketCount
```

If scheduled work/work logs are only reachable through tickets, they do not need to be direct unit blockers separately.

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
  -> delete guard checks ticket dependencies
  -> if scheduled work or work logs exist, return BusinessRuleError
  -> if allowed, _uow.Tickets.RemoveAsync(ticketId, managementCompanyId, cancellationToken)
  -> SaveChangesAsync
```

Ticket dependencies should include:

```text
ScheduledWorkCount
WorkLogCount, through scheduled work
```

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
  -> delete guard checks property dependencies
  -> if dependencies exist, return BusinessRuleError
  -> if allowed, _uow.Properties.RemoveAsync(propertyId, customerId, cancellationToken)
  -> SaveChangesAsync
```

Property dependencies should include:

```text
UnitCount
LeaseCount
TicketCount
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
  -> delete guard checks customer dependencies
  -> if dependencies exist, return BusinessRuleError
  -> if allowed, _uow.Customers.RemoveAsync(customerId, managementCompanyId, cancellationToken)
  -> SaveChangesAsync
```

Customer dependencies should include:

```text
PropertyCount
UnitCount
LeaseCount
TicketCount
CustomerRepresentativeCount
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
  -> delete guard checks resident dependencies
  -> if dependencies exist, return BusinessRuleError
  -> if allowed, _uow.Residents.RemoveAsync(residentId, managementCompanyId, cancellationToken)
  -> SaveChangesAsync
```

Resident dependencies should include:

```text
LeaseCount
TicketCount
ResidentUserCount
ResidentContactCount
CustomerRepresentativeLinkCount
```

---

# Out of scope

Do not implement cascade delete orchestration.

Do not delete related business entities automatically.

Do not rewrite all BLL services at once.

Do not change database schema unless FK behavior is clearly wrong.

Do not handle UI formatting beyond returning useful business errors/results.

Do not introduce a new result/error pattern unrelated to the current `FluentResults` style.

Do not expose raw FK constraint names to users.

---

# Acceptance criteria

Phase 5 is complete when:

- `IAppDeleteGuard` and `AppDeleteGuard` exist.
- Delete guard checks dependencies through repositories and `IAppUOW`.
- Dependency summary methods exist on owning repositories for converted workflows.
- Converted delete methods return `BusinessRuleError` when dependencies exist.
- Converted delete methods delete only the requested entity when allowed.
- Converted delete methods use base `RemoveAsync` where possible.
- Converted repository cascade `DeleteAsync` methods are removed once no longer needed.
- Explicit transaction blocks are removed from simple converted deletes.
- User-facing/business errors clearly explain why deletion is blocked.
- Build succeeds.
