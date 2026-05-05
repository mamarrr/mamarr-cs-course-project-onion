# DAL Refactor Phase 6 — BLL Service Cleanup After DAL Refactor

> Scope file only. Use this together with `DAL_REFACTOR_MASTER_GUIDANCE.md`.
> Do not implement other phases unless explicitly instructed.

## Purpose

Phase 6 is a BLL consistency and cleanup pass after the DAL refactor, repository boundary cleanup, canonical DAL DTO standardization, and delete-guard work.

This phase should not become another large architecture redesign.

The goal is to make sure BLL services consistently use the cleaned DAL architecture:

```text
Repositories answer factual persistence questions.
BLL services make validation, authorization, workflow, and error decisions.
BLL services use the correct owning repository.
Converted delete flows use the delete guard before simple deletion.
Simple create flows use the canonical DAL DTO and the Add return id where practical.
Workflow-heavy services stay workflow-heavy and are not forced into BaseService.
```

---

# Current project context

A lot of the Phase 6 direction already exists in the current codebase:

```text
App.BLL.Contracts exists.
App.BLL.DTO exists.
AppBLL is the BLL composition root.
BLL services are split by feature/workflow.
The delete guard exists.
The main converted delete flows use guard + RemoveAsync.
DAL repositories use canonical DTOs.
BaseRepository.Add returns the created entity id.
```

Therefore Phase 6 should be treated mainly as:

```text
verify consistency
remove transitional leftovers
fix BLL callers
align create/update/delete usage
document intentional exceptions
```

---

# Goals

1. Update BLL services to use cleaned repository boundaries.

2. Keep validation decisions in BLL.

3. Ensure converted delete methods use the delete guard before deleting.

4. Use canonical DAL DTOs when calling repositories.

5. Align simple create flows with `BaseRepository.Add` returning the created id.

6. Prepare for later use of `BaseService` where appropriate, but do not force it.

7. Update controllers/API callers only where required by changed BLL contracts.

---

# Important intentional exception — ManagementCompany DeleteCascadeAsync

`ManagementCompanyRepository.DeleteCascadeAsync` is intentionally left unchanged in this phase.

Do not remove it.

Do not replace it with delete guard.

Do not convert it to `RemoveAsync`.

Do not make Phase 6 changes to this workflow.

It may remain user-facing and may remain available in the UI for now.

The current decision is:

```text
ManagementCompany.DeleteCascadeAsync stays as-is for now.
Management-company cascade deletion is not part of Phase 6 cleanup.
```

This exception is intentional and should not be treated as missed Phase 5 or Phase 6 work.

Phase 6 should only ensure that other BLL cleanup work does not accidentally break this existing management-company delete behavior.

---

# Task 1 — Verify BLL calls the correct owning repositories

BLL services should call factual checks through the repository that owns the entity being checked.

Examples:

```text
Resident checks:
  UOW.Residents

Property checks:
  UOW.Properties

Unit checks:
  UOW.Units

Lease checks:
  UOW.Leases

Lookup/role/status/category checks:
  UOW.Lookups

Ticket checks:
  UOW.Tickets

Customer checks:
  UOW.Customers

Management-company membership/access checks:
  UOW.ManagementCompanies
```

Repositories should answer factual questions such as:

```text
ExistsInCompanyAsync
ExistsInCustomerAsync
ExistsInPropertyAsync
IsLinkedToUnitAsync
TicketNrExistsAsync
RegistryCodeExistsInCompanyAsync
HasOverlappingActiveLeaseAsync
```

BLL should decide what those facts mean:

```text
ValidationAppError
NotFoundError
ForbiddenError
ConflictError
BusinessRuleError
```

Do not move validation decisions back into repositories.

---

# Task 2 — Verify lease service repository ownership

Lease workflows are a key area to verify because older repository methods used to reach too far.

Lease service rules:

```text
Resident existence must be checked through UOW.Residents.
Unit existence must be checked through UOW.Units.
Property existence must be checked through UOW.Properties.
Lease overlap must be checked through UOW.Leases.
Lease role lookup must be checked through UOW.Lookups.
```

Allowed lease repository methods:

```text
AllByResidentAsync
AllByUnitAsync
FirstByIdForResidentAsync
FirstByIdForUnitAsync
HasOverlappingActiveLeaseAsync
UpdateForResidentAsync
UpdateForUnitAsync
DeleteForResidentAsync
DeleteForUnitAsync
```

`DeleteForResidentAsync` and `DeleteForUnitAsync` are allowed to remain if they only delete the lease row after scoped lookup.

Do not force lease deletes into the delete guard unless lease deletion gains independent blocking dependencies.

---

# Task 3 — Verify converted delete flows

For converted entities, BLL delete methods should use this pattern:

```text
1. Resolve access/scope.
2. Confirm entity exists.
3. Validate delete confirmation input.
4. Check permissions.
5. Ask IAppDeleteGuard whether deletion is allowed.
6. If blocked, return generic BusinessRuleError.
7. If allowed, call owning repository RemoveAsync.
8. Save changes.
9. Return success.
```

Converted delete flows:

```text
UnitProfileService.DeleteAsync
PropertyProfileService.DeleteAsync
CustomerProfileService.DeleteAsync
ResidentProfileService.DeleteAsync
ManagementTicketService.DeleteAsync
```

These should not call old cascade delete methods.

Expected converted delete target:

```text
_uow.Units.RemoveAsync(...)
_uow.Properties.RemoveAsync(...)
_uow.Customers.RemoveAsync(...)
_uow.Residents.RemoveAsync(...)
_uow.Tickets.RemoveAsync(...)
```

Blocked delete should return the existing generic business message:

```text
Unable to delete because dependent records exist.
```

or the project resource value for the same message.

---

# Task 4 — Keep ManagementCompany delete behavior unchanged

Management-company deletion is intentionally excluded from the guard conversion.

Do not require this flow to use:

```text
IAppDeleteGuard
HasDeleteDependenciesAsync
RemoveAsync
```

Do not remove:

```text
IManagementCompanyRepository.DeleteCascadeAsync
ManagementCompanyRepository.DeleteCascadeAsync
```

Do not change Web/UI behavior around management-company delete in this phase unless there is a compile error caused by unrelated BLL contract changes.

This method may remain user-facing and UI-accessible for now.

---

# Task 5 — Align simple create flows with Add returning id

`BaseRepository.Add` now returns the created entity id.

For simple create flows, prefer this pattern:

```text
var id = _uow.SomeRepository.Add(new SomeDalDto
{
    ...
});

await _uow.SaveChangesAsync(cancellationToken);

return id;
```

Avoid manually generating ids in BLL unless the id is needed before calling `Add`.

Acceptable reason to pre-generate an id:

```text
The id is needed to build another object before Add.
The workflow must return/use the id before repository Add is called.
A multi-entity workflow needs a shared id for multiple related objects.
```

Otherwise, use the id returned by `Add`.

This is a cleanup consistency task, not a reason to rewrite complex workflows.

---

# Task 6 — Verify canonical DAL DTO usage from BLL

BLL should call DAL repositories with canonical DAL DTOs for simple persistence.

Examples:

```text
CustomerDalDto
PropertyDalDto
UnitDalDto
ResidentDalDto
LeaseDalDto
TicketDalDto
ManagementCompanyDalDto
```

Do not reintroduce DAL create/update DTOs that were removed during Phase 4.

BLL command DTOs and BLL models may still be specialized.

That is fine:

```text
BLL command/model DTOs can be use-case specific.
DAL canonical DTOs should be standard for simple repository persistence.
```

---

# Task 7 — Keep validation in BLL

BLL services should remain responsible for:

```text
required fields
duplicate checks
permission decisions
workflow rules
status transition rules
business-rule errors
validation error construction
confirmation checks
user-facing error messages
```

Repositories should not return `FluentResults`.

Repositories should not construct:

```text
ValidationAppError
BusinessRuleError
ForbiddenError
ConflictError
NotFoundError
```

Repositories should return facts or persistence results.

---

# Task 8 — Controller/API caller alignment

Do not redesign controllers in Phase 6.

However, update WebApp/API controllers where required so they compile against the new BLL contracts and DTOs.

Controller rules:

```text
Controllers may map request/view models to BLL commands.
Controllers may choose redirect/view/API response behavior.
Controllers should not contain business validation that belongs in BLL.
Controllers should not call DAL repositories directly.
Controllers should not bypass BLL delete guard logic.
```

This task is only about caller alignment, not controller architecture redesign.

---

# Task 9 — BaseService usage guidance

Use `BaseService` only for simple CRUD-style BLL services.

Do not force these into `BaseService` in Phase 6:

```text
onboarding services
lease assignment workflows
ticket workflows
profile services with access checks
membership administration
delete guard or delete policy services
workspace/access services
management-company profile/delete cascade workflows
```

Potential future simple candidates:

```text
lookup-like services
simple admin CRUD screens
small aggregate services without complex validation/access logic
```

BaseService adoption is not required for Phase 6 completion.

---

# Out of scope

Do not rewrite repositories unless required to fix a BLL caller.

Do not redesign all service contracts.

Do not redesign controllers.

Do not introduce cascade delete orchestration.

Do not convert management-company `DeleteCascadeAsync`.

Do not remove management-company `DeleteCascadeAsync`.

Do not force workflow-heavy services into `BaseService`.

Do not move validation decisions into repositories.

Do not reintroduce old DAL create/update DTOs.

Do not change database schema unless a real model bug is found.

---

# Suggested verification checklist

Use this checklist when implementing or reviewing Phase 6:

```text
[ ] BLL services call the correct owning repositories.
[ ] Lease service uses Resident/Unit/Property/Lookup repositories for factual checks.
[ ] Converted delete services use IAppDeleteGuard.
[ ] Converted delete services use RemoveAsync after guard allows deletion.
[ ] Converted delete services return BusinessRuleError when guard blocks deletion.
[ ] Converted delete services do not call old cascade delete methods.
[ ] ManagementCompany.DeleteCascadeAsync is intentionally unchanged.
[ ] Simple create flows use Add return id where practical.
[ ] BLL uses canonical DAL DTOs for repository persistence.
[ ] BLL command/model DTOs remain in BLL layer.
[ ] Controllers compile against new BLL contracts.
[ ] Controllers do not call DAL directly.
[ ] No validation decisions were moved into DAL.
[ ] Build succeeds.
```

---

# Acceptance criteria

Phase 6 is complete when:

```text
BLL services call repository methods from the correct owning repositories.
BLL validation and business decisions stay in BLL.
Converted BLL delete methods use the delete guard.
Converted BLL delete methods call base RemoveAsync when deletion is allowed.
Converted BLL delete methods return generic BusinessRuleError when deletion is blocked.
Old cascade delete repository methods are not called by converted Unit/Property/Customer/Resident/Ticket flows.
ManagementCompany.DeleteCascadeAsync remains unchanged by intentional decision.
Simple create flows use Add return ids where practical or document why ids are pre-generated.
BLL services use canonical DAL DTOs for simple persistence.
Controllers/API callers compile against the new BLL contracts where required.
Workflow-heavy services are not forced into BaseService.
Build succeeds.
```

---

# Final reminder

Phase 6 should be a cleanup and consistency phase, not a new rewrite.

Keep the current BLL split, delete guard, canonical DTO usage, and repository ownership boundaries.

Do not change management-company cascade delete behavior in this phase.
