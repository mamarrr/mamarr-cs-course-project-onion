# DAL Refactor Phase 5: Introduce BLL Delete Guard and Blocked-Delete Policy

> Scope file only. Use this together with `DAL_REFACTOR_MASTER_GUIDANCE.md`.
> Do not implement other phases unless explicitly instructed.

## Goal

Replace cross-aggregate cascade delete workflows with blocked-delete validation.

For important business entities, deletion should be blocked when dependent records exist. The user should receive a clear business error explaining why the delete cannot happen.

## Why blocked delete?

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
  -> check dependencies
  -> if dependencies exist, return error
  -> if no dependencies exist, delete only customer
```

## Proposed contracts

Add a BLL delete guard contract.

Recommended starting point:

```text
App.BLL.Contracts/Common/Deletion/IAppDeleteGuard
App.BLL/Services/Common/Deletion/AppDeleteGuard
```

Add dependency summary models in BLL DTO/contracts, for example:

```text
App.BLL.DTO/Common/Deletion/DeleteDependencySummaryModel
App.BLL.DTO/Common/Deletion/DeleteDependencyModel
```

or domain-specific summaries if clearer:

```text
CustomerDeleteDependencySummaryModel
PropertyDeleteDependencySummaryModel
UnitDeleteDependencySummaryModel
ResidentDeleteDependencySummaryModel
TicketDeleteDependencySummaryModel
```

## Responsibilities

The delete guard should:

- Receive ids and scope ids from BLL services.
- Ask owning repositories for dependency summaries/counts.
- Return whether deletion is allowed.
- Return dependency information for user-facing/business errors.
- Avoid using EF Core or `AppDbContext` directly.

The delete guard should not:

- Delete anything.
- Use `AppDbContext` directly.
- Use EF Core APIs directly.
- Know SQL details.
- Be called by repositories.
- Live in `App.DAL.EF`.

## Repository methods needed by the delete guard

Create focused dependency methods on owning repositories.

Examples:

```text
CustomerRepository:
  ExistsInCompanyAsync(customerId, managementCompanyId)
  GetDeleteDependencySummaryAsync(customerId, managementCompanyId)

PropertyRepository:
  ExistsInCustomerAsync(propertyId, customerId)
  GetDeleteDependencySummaryAsync(propertyId, customerId)

UnitRepository:
  ExistsInPropertyAsync(unitId, propertyId)
  GetDeleteDependencySummaryAsync(unitId, propertyId, managementCompanyId)

ResidentRepository:
  ExistsInCompanyAsync(residentId, managementCompanyId)
  GetDeleteDependencySummaryAsync(residentId, managementCompanyId)

TicketRepository:
  GetDeleteDependencySummaryAsync(ticketId, managementCompanyId)

LeaseRepository:
  GetDeleteDependencySummaryAsync(leaseId, managementCompanyId), if leases have blockers
```

A dependency summary should contain counts or boolean flags for records that block deletion.

Examples:

```text
Customer dependencies:
  PropertyCount
  UnitCount
  LeaseCount
  TicketCount
  RepresentativeCount

Property dependencies:
  UnitCount
  LeaseCount
  TicketCount

Unit dependencies:
  LeaseCount
  TicketCount

Resident dependencies:
  LeaseCount
  TicketCount
  ContactCount, if contacts are independent blockers

Ticket dependencies:
  ScheduledWorkCount
  WorkLogCount
```

## BLL behavior

A BLL delete method should follow this flow:

```text
1. Resolve access/scope.
2. Check permissions.
3. Ask IAppDeleteGuard whether the entity can be deleted.
4. If blocked, return a business-rule error with dependency details.
5. If allowed, call the owning repository simple delete method.
6. Save changes.
7. Return success.
```

## Database FK safety net

The database should still prevent invalid deletes through FK constraints.

If an FK violation still occurs:

- Catch it at a suitable boundary if the project already has exception handling patterns.
- Return a generic blocked-delete business error.
- Do not expose raw FK constraint names to users.

Do not rely only on FK exceptions for normal UX. Prefer explicit dependency checks through the delete guard.

## First workflows to convert

Convert delete behavior in this order:

1. Unit delete
2. Property delete
3. Customer delete
4. Resident delete
5. Ticket delete
6. Lease delete, if lease deletion has blockers

Start with one workflow and build before continuing.

## Out of scope

- Do not implement cascade delete orchestration.
- Do not delete related business entities automatically.
- Do not rewrite all BLL services at once.
- Do not change database schema unless FK behavior is clearly wrong.
- Do not handle UI formatting beyond returning useful business errors/results.

## Acceptance criteria

- BLL delete guard contract and implementation exist.
- Delete guard checks dependencies through repositories.
- At least the first selected delete workflow blocks when dependencies exist.
- Repositories no longer need mega-delete methods for that converted workflow.
- The converted delete method deletes only the requested entity when allowed.
- User-facing/business errors are clear enough to explain why deletion is blocked.
- Build succeeds.
