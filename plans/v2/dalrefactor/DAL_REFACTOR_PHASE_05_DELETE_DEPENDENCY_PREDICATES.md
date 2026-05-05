# DAL Refactor Phase 3 Delete Dependency Predicate Inventory

Scope: Phase 3 artifact for `DAL_REFACTOR_PHASE_03_REPOSITORY_METHOD_OWNERSHIP_CLEANUP.md`.

Do not implement these guards in Phase 3. Use this list as input for Phase 5 blocked-delete work.

## Repository ownership

CustomerRepository:
- `ExistsInCompanyAsync(customerId, managementCompanyId)`
- `OptionsForTicketAsync(managementCompanyId)` already owns ticket customer selector data.
- `GetDeleteDependencySummaryAsync(customerId, managementCompanyId)`
- Summary counts: properties, units, leases, tickets, customer representatives.

PropertyRepository:
- `ExistsInCustomerAsync(propertyId, customerId)`
- `ExistsInCompanyAsync(propertyId, managementCompanyId)` already exists for tenant-scoped validation.
- `OptionsForTicketAsync(managementCompanyId, customerId?)` already owns ticket property selector data.
- `SearchForLeaseAssignmentAsync(managementCompanyId, searchTerm)` already owns lease-assignment property search data.
- `GetDeleteDependencySummaryAsync(propertyId, customerId, managementCompanyId)`
- Summary counts: units, leases, tickets.

UnitRepository:
- `ExistsInPropertyAsync(unitId, propertyId)`
- `ExistsInCompanyAsync(unitId, managementCompanyId)` already exists for tenant-scoped validation.
- `OptionsForTicketAsync(managementCompanyId, propertyId?)` already owns ticket unit selector data.
- `ListForLeaseAssignmentAsync(propertyId, managementCompanyId)` already owns lease-assignment unit selector data.
- `GetDeleteDependencySummaryAsync(unitId, propertyId, managementCompanyId)`
- Summary counts: leases, tickets.

ResidentRepository:
- `ExistsInCompanyAsync(residentId, managementCompanyId)` already exists for tenant-scoped validation.
- `IsLinkedToUnitAsync(residentId, unitId)` already owns resident/unit active lease relationship validation for ticket rules.
- `OptionsForTicketAsync(managementCompanyId, unitId?)` already owns ticket resident selector data.
- `SearchForLeaseAssignmentAsync(managementCompanyId, searchTerm)` already owns lease-assignment resident search data.
- `GetDeleteDependencySummaryAsync(residentId, managementCompanyId)`
- Summary counts: leases, tickets, resident users, resident contacts, customer representative links.

TicketRepository:
- `ExistsInCompanyAsync(ticketId, managementCompanyId)`
- `GetDeleteDependencySummaryAsync(ticketId, managementCompanyId)`
- Summary counts: scheduled work, work logs through scheduled work.
- Ticket lookup, selector option, and cross-reference validation ownership has moved out of `TicketRepository`.

LeaseRepository:
- `GetDeleteDependencySummaryAsync(leaseId, managementCompanyId)` only if lease delete gains independent blockers.
- Current lease deletes remove only the lease row after scoped lookup.
- Lease assignment selector data is no longer owned by `LeaseRepository`.

LookupRepository:
- `FindTicketStatusByCodeAsync(code)` already owns ticket status lookup by code.
- `FindTicketStatusByIdAsync(statusId)` already owns ticket status lookup by id.
- `TicketCategoryExistsAsync(categoryId)` already owns ticket category validation.
- `TicketPriorityExistsAsync(priorityId)` already owns ticket priority validation.
- `TicketStatusExistsAsync(statusId)` already owns ticket status validation.
- `AllTicketStatusesAsync`, `AllTicketPrioritiesAsync`, and `AllTicketCategoriesAsync` already own ticket lookup selector data.
- `ListLeaseRolesAsync()` already owns lease role selector data.
- Property type and management-company role lookup methods are already centralized here.

VendorRepository:
- `ExistsInCompanyAsync(vendorId, managementCompanyId)` already owns ticket vendor validation.
- `OptionsForTicketAsync(managementCompanyId, categoryId?)` already owns ticket vendor selector data.
- `GetDeleteDependencySummaryAsync(vendorId, managementCompanyId)` should be added in Phase 5 if vendor delete is included.
- Summary counts: tickets, scheduled work, vendor contacts, vendor ticket category links.

ManagementCompanyRepository:
- `GetDeleteDependencySummaryAsync(managementCompanyId)`
- Summary counts: customers, properties, units, residents, resident users, resident contacts, customer representatives, tickets, scheduled work, work logs, leases, vendor relationships, contacts, memberships, join requests.

ContactRepository:
- `GetDeleteDependencySummaryAsync(contactId)`
- Summary counts: resident contact links, vendor links, management company links, or other contact ownership links present when Phase 5 starts.

## Phase 3 notes

- Existing cross-aggregate cascade delete methods remain unchanged in this phase.
- Dependency summaries should be owned by the repository for the entity being deleted.
- The BLL delete guard introduced in Phase 5 should compose these repository predicates through `IAppUOW`.
