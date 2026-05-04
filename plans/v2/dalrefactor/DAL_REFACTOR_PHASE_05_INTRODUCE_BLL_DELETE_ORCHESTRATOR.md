# DAL Refactor Phase 5: Introduce BLL delete orchestrator

> Scope file only. Use this together with `DAL_REFACTOR_MASTER_GUIDANCE.md`.
> Do not implement other phases unless explicitly instructed.

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
