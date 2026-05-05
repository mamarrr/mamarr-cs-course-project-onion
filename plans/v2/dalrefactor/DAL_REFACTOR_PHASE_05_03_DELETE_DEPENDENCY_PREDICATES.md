# DAL Refactor Phase 3 Delete Dependency Predicate Inventory

Scope: Phase 3 artifact for `DAL_REFACTOR_PHASE_03_REPOSITORY_METHOD_OWNERSHIP_CLEANUP.md`.

Do not implement these guards in Phase 3. Use this list as input for Phase 5 blocked-delete work.

## Repository ownership

CustomerRepository:
- `ExistsInCompanyAsync(customerId, managementCompanyId)`
- `GetDeleteDependencySummaryAsync(customerId, managementCompanyId)`
- Summary counts: properties, units, leases, tickets, customer representatives.

PropertyRepository:
- `ExistsInCustomerAsync(propertyId, customerId)`
- `ExistsInCompanyAsync(propertyId, managementCompanyId)` already exists for tenant-scoped validation.
- `GetDeleteDependencySummaryAsync(propertyId, customerId, managementCompanyId)`
- Summary counts: units, leases, tickets.

UnitRepository:
- `ExistsInPropertyAsync(unitId, propertyId)`
- `ExistsInCompanyAsync(unitId, managementCompanyId)` already exists for tenant-scoped validation.
- `GetDeleteDependencySummaryAsync(unitId, propertyId, managementCompanyId)`
- Summary counts: leases, tickets.

ResidentRepository:
- `ExistsInCompanyAsync(residentId, managementCompanyId)` already exists for tenant-scoped validation.
- `GetDeleteDependencySummaryAsync(residentId, managementCompanyId)`
- Summary counts: leases, tickets, resident users, resident contacts, customer representative links.

TicketRepository:
- `ExistsInCompanyAsync(ticketId, managementCompanyId)`
- `GetDeleteDependencySummaryAsync(ticketId, managementCompanyId)`
- Summary counts: scheduled work, work logs through scheduled work.

LeaseRepository:
- `GetDeleteDependencySummaryAsync(leaseId, managementCompanyId)` only if lease delete gains independent blockers.
- Current lease deletes remove only the lease row after scoped lookup.

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
