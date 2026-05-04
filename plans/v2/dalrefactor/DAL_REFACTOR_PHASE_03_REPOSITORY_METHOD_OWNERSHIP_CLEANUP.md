# DAL Refactor Phase 3: Repository method ownership cleanup

> Scope file only. Use this together with `DAL_REFACTOR_MASTER_GUIDANCE.md`.
> Do not implement other phases unless explicitly instructed.

### Goals

- Repositories should not reach too far outside their natural DbSet.
- Simple existence checks should live in the repository that owns the checked entity.
- Similar semantic methods should be consolidated.

### General rule

Move methods according to the entity they primarily query:

```text
Resident existence/checks  -> ResidentRepository
Property existence/checks  -> PropertyRepository
Unit existence/checks      -> UnitRepository
Lease existence/checks     -> LeaseRepository
Ticket existence/checks    -> TicketRepository
Lookup/role checks         -> LookupRepository
```

### Lease repository cleanup

Move these methods out of `ILeaseRepository` / `LeaseRepository`:

- `ResidentExistsInCompanyAsync` -> `IResidentRepository` / `ResidentRepository`
- `PropertyExistsInCompanyAsync` -> `IPropertyRepository` / `PropertyRepository`
- `UnitExistsInCompanyAsync` -> `IUnitRepository` / `UnitRepository`
- `LeaseRoleExistsAsync` -> `ILookupRepository` / `LookupRepository`, if lease roles are lookup data

Keep this in `ILeaseRepository` / `LeaseRepository`:

- `HasOverlappingActiveLeaseAsync`
- lease list/detail methods
- lease create/update/delete methods that operate on `Leases`

Review these later; they may be moved after first cleanup:

- `SearchPropertiesAsync` may belong in `PropertyRepository` as a lease-assignment search query.
- `SearchResidentsAsync` may belong in `ResidentRepository` as a lease-assignment search query.
- `ListUnitsForPropertyAsync` may belong in `UnitRepository`.
- `ListLeaseRolesAsync` may belong in `LookupRepository`.

### Similar semantics audit

Search all repositories for methods with similar names or behavior:

- `ExistsInCompanyAsync`
- `ExistsInCustomerAsync`
- `SlugExists...`
- `RegistryCodeExists...`
- `AllSlugs...`
- `FindActive...Context...`
- `Find...RoleCode...`
- `Search...`
- `List...Options...`
- `Delete...`

For each duplicate semantic pattern:

1. Keep the method in the repository that owns the queried entity.
2. Standardize naming where possible.
3. Avoid two repositories implementing the same predicate over the same table.
4. Update BLL services to call the new repository location through `IAppUOW`.
5. Remove old methods from old repository contracts and implementations.

### Suggested naming conventions

Use simple, consistent names:

```text
ExistsInCompanyAsync(entityId, managementCompanyId)
ExistsInCustomerAsync(entityId, customerId)
ExistsInPropertyAsync(entityId, propertyId)
SlugExistsInCompanyAsync(...)
SlugExistsInCustomerAsync(...)
AllSlugsByParentAsync(...)
SearchForLeaseAssignmentAsync(...)
ListForLeaseAssignmentAsync(...)
```

Avoid names that include the caller use case unless the query is truly use-case-specific.

### Acceptance criteria

- `LeaseRepository` no longer owns resident/property/unit existence predicates.
- Each moved method is exposed on the correct repository contract.
- BLL services compile against the new method locations.
- No duplicate method with the same semantics remains in multiple repositories unless there is a clear reason.
