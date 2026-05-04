# DAL Refactor Phase 1 Repository Method Classification

Scope: Phase 1 audit artifact for `DAL_REFACTOR_PHASE_01_STABILIZE_BASE_REPOSITORY_USAGE.md`.

## Base Repository Decisions

- `IBaseRepository` uses `parentId` for optional parent/scope filtering.
- Base async read/delete methods now accept optional `CancellationToken`.
- `RemoveAsync(id, parentId, cancellationToken)` preserves unscoped behavior when `parentId` is omitted, and uses the existing base parent-scope filter when provided.
- `BaseRepository` remains generic CRUD only. No app-specific joins, cascade delete workflows, or domain-specific predicates were added.
- Reads continue to rely on global `QueryTrackingBehavior.NoTrackingWithIdentityResolution`; no new `AsNoTracking()` calls were added.
- Base delete-by-id loading uses `AsTracking()` because it loads an entity and mutates its state.

## ContactRepository

Generic CRUD candidates:
- `FindAsync(contactId, cancellationToken)`
- `AddAsync(ContactCreateDalDto, cancellationToken)`
- `UpdateAsync(ContactUpdateDalDto, cancellationToken)`
- `DeleteAsync(contactId, cancellationToken)`

Projection/search methods:
- None beyond the CRUD-shaped DTO projection in `FindAsync`.

Cross-aggregate delete workflows:
- None. Delete currently removes only `Contact`.

## CustomerRepository

Generic CRUD candidates:
- `AddAsync(CustomerCreateDalDto, cancellationToken)`
- `UpdateProfileAsync(CustomerUpdateDalDto, cancellationToken)`

Projection/search methods:
- `AllByCompanySlugAsync`
- `AllByCompanyIdAsync`
- `AllPropertyLinksByCompanyIdAsync`
- `FirstWorkspaceByCompanyAndSlugAsync`
- `ActiveUserCustomerContextsAsync`
- `ActiveUserCustomerContextExistsAsync`
- `FirstProfileByCompanyAndSlugAsync`
- `FindProfileAsync`
- `FindActiveManagementCompanyRoleCodeAsync`

Validation/existence methods:
- `CustomerSlugExistsInCompanyAsync`
- `RegistryCodeExistsInCompanyAsync`

Cross-aggregate delete workflows:
- `DeleteAsync(customerId, managementCompanyId, cancellationToken)` currently deletes tickets, leases, units, properties, customer representatives, and orphaned contacts before deleting the customer.
- Helper workflows: `DeleteTicketsAsync`, `DeleteContactsIfOrphanedAsync`.

## LeaseRepository

Generic CRUD candidates:
- `AddAsync(LeaseCreateDalDto, cancellationToken)`
- `UpdateForResidentAsync`
- `UpdateForUnitAsync`
- `DeleteForResidentAsync`
- `DeleteForUnitAsync`

Projection/search methods:
- `AllByResidentAsync`
- `AllByUnitAsync`
- `FirstByIdForResidentAsync`
- `FirstByIdForUnitAsync`
- `SearchPropertiesAsync`
- `ListUnitsForPropertyAsync`
- `SearchResidentsAsync`
- `ListLeaseRolesAsync`
- Helper query: `LeaseDetailsQuery`

Validation/existence methods:
- `LeaseRoleExistsAsync`
- `UnitExistsInCompanyAsync`
- `ResidentExistsInCompanyAsync`
- `PropertyExistsInCompanyAsync`
- `HasOverlappingActiveLeaseAsync`

Cross-aggregate delete workflows:
- None. Delete methods remove only `Lease`, scoped through resident/unit context.

Ownership notes for later phases:
- `UnitExistsInCompanyAsync`, `ResidentExistsInCompanyAsync`, and `PropertyExistsInCompanyAsync` query other aggregate roots and are candidates for repository ownership cleanup.

## LookupRepository

Generic CRUD candidates:
- None. This repository does not inherit `BaseRepository`.

Projection/search methods:
- `FindManagementCompanyJoinRequestStatusByCodeAsync`
- `AllManagementCompanyJoinRequestStatusesAsync`
- `FindManagementCompanyRoleByCodeAsync`
- `FindCustomerRepresentativeRoleByCodeAsync`
- `FindLeaseRoleByCodeAsync`
- `FindPropertyTypeByCodeAsync`
- `FindContactTypeByCodeAsync`
- Helper query: `FindByCodeAsync<TLookup>`

Cross-aggregate delete workflows:
- None.

## ManagementCompanyJoinRequestRepository

Generic CRUD candidates:
- `AddJoinRequest(ManagementCompanyJoinRequestCreateDalDto)`
- `SetStatusAsync`

Projection/search methods:
- `PendingByCompanyAsync`
- `FindByIdAndCompanyAsync`
- Helper query: `RequestQuery`

Validation/existence methods:
- `HasPendingRequestAsync`

Cross-aggregate delete workflows:
- None.

## ManagementCompanyRepository

Generic CRUD candidates:
- `FirstBySlugAsync`
- `FirstActiveByRegistryCodeAsync`
- `AddManagementCompanyAsync`
- `UpdateProfileAsync`

Projection/search methods:
- `FindActiveUserRoleCodeAsync`
- `FirstProfileBySlugAsync`
- `FirstProfileByIdAsync`
- `AllSlugsAsync`
- `ActiveUserManagementContextsAsync`
- `ActiveUserManagementContextByCompanyIdAsync`
- `ActiveUserManagementContextExistsBySlugAsync`
- `FirstMembershipByUserAndCompanyAsync`
- `MembersByCompanyAsync`
- `FindMemberByIdAndCompanyAsync`
- `FindMembersByIdsAndCompanyAsync`
- `AllManagementCompanyRolesAsync`
- `FindManagementCompanyRoleByIdAsync`
- `FindAppUserIdByEmailAsync`
- Helper query: `MembershipQuery`

Validation/existence methods:
- `RegistryCodeExistsAsync`
- `UserBelongsToCompanyAsync`
- `MembershipExistsAsync`
- `RegistryCodeExistsOutsideCompanyAsync`
- `CountEffectiveOwnersAsync`

Custom child-row mutation methods:
- `AddMembership`
- `ApplyMembershipUpdateAsync`
- `RemoveMembershipAsync`
- `SetMembershipRoleAsync`

Cross-aggregate delete workflows:
- `DeleteCascadeAsync(managementCompanyId, cancellationToken)` currently deletes tickets, scheduled work/work logs, leases, resident contacts, resident users, residents, customer representatives, units, properties, customers, vendor relationships, contacts, membership rows, join requests, and the management company.
- Helper workflows: `DeleteTicketsAsync`, `DeleteContactsIfOrphanedAsync`.

## PropertyRepository

Generic CRUD candidates:
- `AddAsync(PropertyCreateDalDto, cancellationToken)`
- `UpdateProfileAsync(PropertyUpdateDalDto, cancellationToken)`

Projection/search methods:
- `AllByCustomerAsync`
- `AllPropertyTypeOptionsAsync`
- `FirstWorkspaceByCustomerAndSlugAsync`
- `FindProfileAsync`

Validation/existence methods:
- `PropertyTypeExistsAsync`
- `SlugExistsForCustomerAsync`

Cross-aggregate delete workflows:
- `DeleteAsync(propertyId, customerId, managementCompanyId, cancellationToken)` currently deletes tickets, leases, and units before deleting the property.
- Helper workflow: `DeleteTicketsAsync`.

## ResidentRepository

Generic CRUD candidates:
- `AddAsync(ResidentCreateDalDto, cancellationToken)`
- `UpdateAsync(ResidentUpdateDalDto, cancellationToken)`

Projection/search methods:
- `FirstProfileAsync`
- `FindProfileAsync`
- `AllByCompanyAsync`
- `FirstActiveUserResidentContextAsync`
- `HasActiveUserResidentContextAsync`
- `ContactsByResidentAsync`
- `LeaseSummariesByResidentAsync`

Validation/existence methods:
- `IdCodeExistsForCompanyAsync`

Cross-aggregate delete workflows:
- `DeleteAsync(residentId, managementCompanyId, cancellationToken)` currently deletes tickets, leases, resident contacts, resident users, customer representatives, and orphaned contacts before deleting the resident.
- Helper workflows: `DeleteTicketsAsync`, `DeleteContactsIfOrphanedAsync`.

## TicketRepository

Generic CRUD candidates:
- `AddAsync(TicketCreateDalDto, cancellationToken)`
- `UpdateAsync(TicketUpdateDalDto, cancellationToken)`
- `UpdateStatusAsync(TicketStatusUpdateDalDto, cancellationToken)`

Projection/search methods:
- `AllByCompanyAsync`
- `FindDetailsAsync`
- `FindForEditAsync`
- `GetNextTicketNrAsync`
- `FindStatusByCodeAsync`
- `FindStatusByIdAsync`
- `AllStatusesAsync`
- `AllPrioritiesAsync`
- `AllCategoriesAsync`
- `CustomerOptionsAsync`
- `PropertyOptionsAsync`
- `UnitOptionsAsync`
- `ResidentOptionsAsync`
- `VendorOptionsAsync`
- `ValidateReferencesAsync`
- Helper projection: `LookupOptions<TLookup>`

Validation/existence methods:
- `TicketNrExistsAsync`

Cross-aggregate delete workflows:
- `DeleteAsync(ticketId, managementCompanyId, cancellationToken)` currently deletes work logs and scheduled work before deleting the ticket.

## UnitRepository

Generic CRUD candidates:
- `AddAsync(UnitCreateDalDto, cancellationToken)`
- `UpdateAsync(UnitUpdateDalDto, cancellationToken)`

Projection/search methods:
- `FirstDashboardAsync`
- `FirstProfileAsync`
- `FindProfileAsync`
- `AllByPropertyAsync`
- `AllSlugsByPropertyWithPrefixAsync`
- Helper query: `BaseScopedUnitQuery`

Validation/existence methods:
- `UnitSlugExistsForPropertyAsync`

Cross-aggregate delete workflows:
- `DeleteAsync(unitId, propertyId, managementCompanyId, cancellationToken)` currently deletes tickets and leases before deleting the unit.
- Helper workflow: `DeleteTicketsAsync`.
