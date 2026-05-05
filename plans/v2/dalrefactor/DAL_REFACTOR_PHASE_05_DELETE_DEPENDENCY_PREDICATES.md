# DAL Refactor Phase 5 Delete Dependency Predicate Inventory

Scope: Phase 5 input artifact for blocked-delete work.

Use this together with:

```text
DAL_REFACTOR_PHASE_05_INTRODUCE_BLL_DELETE_GUARD_AND_BLOCKED_DELETE_POLICY.md
DAL_REFACTOR_MASTER_GUIDANCE.md
```

## Purpose

This file lists repository-owned boolean dependency predicates needed by the BLL delete guard.

The rule is:

```text
Repository for the entity being deleted owns the delete dependency predicate.
BLL delete guard composes those predicates.
BLL services use the guard before calling RemoveAsync.
```

This file intentionally does **not** require counts.

The UI does not need to know how many dependencies exist and does not need to know which exact dependency type blocked deletion.

A generic error is enough:

```text
Unable to delete because dependent records exist.
```

---

# General repository method shape

Prefer one boolean dependency predicate per deletable aggregate/entity:

```text
HasDeleteDependenciesAsync(entityId, scopeId..., cancellationToken)
```

The method should return:

```text
Task<bool>
```

Meaning:

```text
true  -> at least one dependency exists; deletion must be blocked
false -> no known dependency exists; deletion may proceed
```

Use efficient `AnyAsync(...)` queries.

Do not use `CountAsync(...)` unless there is a real implementation reason. Counting is unnecessary for Phase 5.

The predicate should not:

- delete anything;
- return full child entities;
- return dependency counts;
- return dependency names;
- perform BLL authorization;
- format UI messages;
- return BLL DTOs.

---

# Conversion rule after predicate exists

Once a workflow has:

```text
repository dependency predicate
BLL delete guard check
BLL generic blocked-delete error
```

then the actual delete should use:

```text
BaseRepository.RemoveAsync(id, parentId, cancellationToken)
```

where possible.

Remove old custom cascade `DeleteAsync(...)` methods from the repository contract and implementation once the converted BLL workflow no longer uses them.

---

# UnitRepository

Existing ownership:

```text
ExistsInPropertyAsync(unitId, propertyId)
ExistsInCompanyAsync(unitId, managementCompanyId)
OptionsForTicketAsync(managementCompanyId, propertyId?)
ListForLeaseAssignmentAsync(propertyId, managementCompanyId)
```

Add in Phase 5A:

```text
HasDeleteDependenciesAsync(unitId, propertyId, managementCompanyId, cancellationToken)
```

Predicate should return `true` if any of these exist:

```text
lease linked to the unit
ticket linked to the unit
```

Notes:

```text
Scheduled work and work logs do not need separate direct unit checks if they are only reachable through tickets.
If any ticket exists for the unit, deletion is blocked.
```

Old behavior to remove after conversion:

```text
IUnitRepository.DeleteAsync(unitId, propertyId, managementCompanyId)
UnitRepository.DeleteAsync(...)
```

New delete target after guard passes:

```text
_uow.Units.RemoveAsync(unitId, propertyId, cancellationToken)
```

---

# TicketRepository

Existing ownership:

```text
AllByCompanyAsync(...)
FindDetailsAsync(...)
FindForEditAsync(...)
GetNextTicketNrAsync(...)
TicketNrExistsAsync(...)
UpdateStatusAsync(...)
```

Add in Phase 5A:

```text
ExistsInCompanyAsync(ticketId, managementCompanyId, cancellationToken), if useful
HasDeleteDependenciesAsync(ticketId, managementCompanyId, cancellationToken)
```

Predicate should return `true` if any of these exist:

```text
scheduled work linked to the ticket
work log linked through scheduled work
```

Old behavior to remove after conversion:

```text
ITicketRepository.DeleteAsync(ticketId, managementCompanyId)
TicketRepository.DeleteAsync(...)
```

New delete target after guard passes:

```text
_uow.Tickets.RemoveAsync(ticketId, managementCompanyId, cancellationToken)
```

Notes:

```text
Do not delete scheduled work or work logs automatically.
If scheduled work or work logs exist, block ticket deletion.
```

---

# PropertyRepository

Existing ownership:

```text
ExistsInCustomerAsync(propertyId, customerId)
ExistsInCompanyAsync(propertyId, managementCompanyId)
OptionsForTicketAsync(managementCompanyId, customerId?)
SearchForLeaseAssignmentAsync(managementCompanyId, searchTerm)
```

Add in Phase 5B:

```text
HasDeleteDependenciesAsync(propertyId, customerId, managementCompanyId, cancellationToken)
```

Predicate should return `true` if any of these exist:

```text
unit linked to the property
lease linked through property units
ticket linked to the property
ticket linked through property units
```

Old behavior to remove after conversion:

```text
IPropertyRepository.DeleteAsync(propertyId, customerId, managementCompanyId)
PropertyRepository.DeleteAsync(...)
```

New delete target after guard passes:

```text
_uow.Properties.RemoveAsync(propertyId, customerId, cancellationToken)
```

---

# CustomerRepository

Existing ownership:

```text
ExistsInCompanyAsync(customerId, managementCompanyId)
OptionsForTicketAsync(managementCompanyId)
FindActiveManagementCompanyRoleCodeAsync(...)
RegistryCodeExistsInCompanyAsync(...)
```

Add in Phase 5B:

```text
HasDeleteDependenciesAsync(customerId, managementCompanyId, cancellationToken)
```

Predicate should return `true` if any of these exist:

```text
property linked to the customer
unit linked through customer properties
lease linked through customer properties/units
ticket linked directly to the customer
ticket linked through customer properties/units
customer representative linked to the customer
```

Old behavior to remove after conversion:

```text
ICustomerRepository.DeleteAsync(customerId, managementCompanyId)
CustomerRepository.DeleteAsync(...)
```

New delete target after guard passes:

```text
_uow.Customers.RemoveAsync(customerId, managementCompanyId, cancellationToken)
```

---

# ResidentRepository

Existing ownership:

```text
ExistsInCompanyAsync(residentId, managementCompanyId)
IsLinkedToUnitAsync(residentId, unitId)
OptionsForTicketAsync(managementCompanyId, unitId?)
SearchForLeaseAssignmentAsync(managementCompanyId, searchTerm)
IdCodeExistsForCompanyAsync(...)
```

Add in Phase 5B:

```text
HasDeleteDependenciesAsync(residentId, managementCompanyId, cancellationToken)
```

Predicate should return `true` if any of these exist:

```text
lease linked to the resident
ticket linked to the resident
resident user linked to the resident
resident contact linked to the resident
customer representative link involving the resident
```

Old behavior to remove after conversion:

```text
IResidentRepository.DeleteAsync(residentId, managementCompanyId)
ResidentRepository.DeleteAsync(...)
```

New delete target after guard passes:

```text
_uow.Residents.RemoveAsync(residentId, managementCompanyId, cancellationToken)
```

Notes:

```text
Resident contact links should block resident deletion unless the business decides links are owned purely by the resident and may be removed separately by the user first.
Resident user links should block deletion because they represent account access/history.
```

---

# LeaseRepository

Existing ownership:

```text
AllByResidentAsync(...)
AllByUnitAsync(...)
FirstByIdForResidentAsync(...)
FirstByIdForUnitAsync(...)
HasOverlappingActiveLeaseAsync(...)
UpdateForResidentAsync(...)
UpdateForUnitAsync(...)
DeleteForResidentAsync(...)
DeleteForUnitAsync(...)
```

Add only if lease delete gains independent blockers:

```text
HasDeleteDependenciesAsync(leaseId, managementCompanyId, cancellationToken)
```

Predicate should return `true` only if a real lease dependency exists, for example:

```text
ticket linked directly to lease, only if such relation exists
scheduled work linked directly to lease, only if such relation exists
```

Current guidance:

```text
If lease delete only removes the lease row after scoped lookup and no independent dependencies exist,
do not overbuild a lease delete guard in Phase 5A/B.
```

---

# VendorRepository

Existing ownership:

```text
ExistsInCompanyAsync(vendorId, managementCompanyId)
OptionsForTicketAsync(managementCompanyId, categoryId?)
```

Add in Phase 5C if vendor delete exists:

```text
HasDeleteDependenciesAsync(vendorId, managementCompanyId, cancellationToken)
```

Predicate should return `true` if any of these exist:

```text
ticket linked to the vendor
scheduled work linked to the vendor
vendor contact link
vendor ticket category link
```

New delete target after guard passes:

```text
_uow.Vendors.RemoveAsync(vendorId, managementCompanyId, cancellationToken)
```

Notes:

```text
Vendor deletion should be blocked if tickets or scheduled work still reference the vendor.
Vendor contact/category links should block unless a separate unlink workflow exists and has been run first.
```

---

# ManagementCompanyRepository

Existing ownership:

```text
membership/access queries
workspace queries
management company profile queries
role and ownership workflows
```

Add in Phase 5C only if management company deletion is supported:

```text
HasDeleteDependenciesAsync(managementCompanyId, cancellationToken)
```

Predicate should return `true` if any dependent data exists, including but not limited to:

```text
customers
properties
units
residents
resident users
resident contacts
customer representatives
tickets
scheduled work
work logs
leases
vendors
vendor contacts
vendor ticket category links
contacts
memberships
join requests
```

Notes:

```text
Management company deletion is a high-impact workflow.
Do not implement it early unless there is a real UI/business need.
Blocked-delete behavior is strongly preferred.
```

---

# ContactRepository

Add in Phase 5C only if contacts become independently deletable:

```text
HasDeleteDependenciesAsync(contactId, managementCompanyId?, cancellationToken)
```

Predicate should return `true` if any of these exist:

```text
resident contact link
vendor contact link
management company link, if applicable
other contact ownership link, if applicable
```

Notes:

```text
If contacts are only managed through resident/vendor/company workflows,
do not implement independent contact deletion yet.
```

---

# LookupRepository

Existing ownership:

```text
FindTicketStatusByCodeAsync(code)
FindTicketStatusByIdAsync(statusId)
TicketCategoryExistsAsync(categoryId)
TicketPriorityExistsAsync(priorityId)
TicketStatusExistsAsync(statusId)
AllTicketStatusesAsync
AllTicketPrioritiesAsync
AllTicketCategoriesAsync
ListLeaseRolesAsync()
property type lookup methods
management company role lookup methods
```

Phase 5 guidance:

```text
Do not add delete dependency predicates for lookup delete unless lookup deletion becomes a supported admin workflow.
Lookup rows should normally be seeded/static and not user-deleted.
```

---

# Delete guard composition

The BLL delete guard should expose focused methods such as:

```text
CanDeleteUnitAsync(unitId, propertyId, managementCompanyId, cancellationToken)
CanDeleteTicketAsync(ticketId, managementCompanyId, cancellationToken)
CanDeletePropertyAsync(propertyId, customerId, managementCompanyId, cancellationToken)
CanDeleteCustomerAsync(customerId, managementCompanyId, cancellationToken)
CanDeleteResidentAsync(residentId, managementCompanyId, cancellationToken)
```

Each method should:

```text
call the matching repository HasDeleteDependenciesAsync(...)
return false if dependency exists
return true if no dependency exists
```

Do not make the guard depend on EF Core.

Do not make the guard delete anything.

Do not make the guard build dependency count summaries.

---

# Implementation order

Use this order:

```text
Phase 5A:
1. Unit delete
2. Ticket delete

Phase 5B:
3. Property delete
4. Customer delete
5. Resident delete

Phase 5C:
6. Vendor delete, if present
7. Management company delete, if supported
8. Contact delete, if present
9. Lease delete, if blockers exist
```

After each slice:

```text
1. Add repository predicate method.
2. Implement predicate query with AnyAsync.
3. Add delete guard method.
4. Convert BLL delete service.
5. Replace cascade DeleteAsync with base RemoveAsync.
6. Return generic BusinessRuleError on blocked delete.
7. Remove obsolete custom DeleteAsync from repository contract/implementation.
8. Build.
9. Commit/checkpoint.
```

---

# Acceptance criteria for this inventory

The inventory is complete when:

```text
Every converted delete workflow has a boolean dependency predicate.
Every dependency predicate belongs to the repository for the entity being deleted.
BLL delete guard composes predicates through IAppUOW.
Converted deletes use base RemoveAsync after guard passes.
Old cascade DeleteAsync methods are removed once unused.
No dependency counts are required or returned.
```
