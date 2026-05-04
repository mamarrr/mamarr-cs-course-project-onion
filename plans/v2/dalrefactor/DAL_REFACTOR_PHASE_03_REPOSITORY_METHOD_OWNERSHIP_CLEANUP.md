# DAL Refactor Phase 3: Repository Method Ownership Cleanup

> Scope file only. Use this together with `DAL_REFACTOR_MASTER_GUIDANCE.md`.
> Do not implement other phases unless explicitly instructed.

## Goals

- Repositories should not reach too far outside their natural DbSet.
- Simple existence checks should live in the repository that owns the checked entity.
- Similar semantic methods should be consolidated.
- Prepare repositories for blocked-delete dependency checks, but do not implement the BLL delete guard yet.

## General rule

Move methods according to the entity they primarily query:

```text
Resident existence/checks  -> ResidentRepository
Property existence/checks  -> PropertyRepository
Unit existence/checks      -> UnitRepository
Lease existence/checks     -> LeaseRepository
Ticket existence/checks    -> TicketRepository
Lookup/role checks         -> LookupRepository
```

## Lease repository cleanup

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

## Similar semantics audit

Search all repositories for methods with similar names or behavior:

- `ExistsInCompanyAsync`
- `ExistsInCustomerAsync`
- `ExistsInPropertyAsync`
- `HasDeleteDependenciesAsync`
- `GetDeleteDependencySummaryAsync`
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

## Delete-related cleanup in this phase

Do not implement the delete guard yet, but begin preparing for it by identifying where dependency checks should live.

Examples:

```text
CustomerRepository:
  should be able to report customer delete dependencies owned by customer scope

PropertyRepository:
  should be able to report whether property has units/tickets/other blockers if those are property-owned checks

UnitRepository:
  should be able to report whether unit has leases/tickets

ResidentRepository:
  should be able to report whether resident has leases/tickets/contacts where appropriate

TicketRepository:
  should be able to report whether ticket has scheduled work/work logs
```

Keep the actual BLL delete policy implementation for Phase 5.

## Out of scope

- Do not create `IAppDeleteGuard` yet.
- Do not replace existing delete workflows yet.
- Do not implement user-facing blocked-delete messages yet.
- Do not refactor easy CRUD yet unless required by method relocation.

## Acceptance criteria

- `LeaseRepository` no longer owns resident/property/unit existence predicates.
- Each moved method is exposed on the correct repository contract.
- BLL services compile against the new method locations.
- No duplicate method with the same semantics remains in multiple repositories unless there is a clear reason.
- A list of delete dependency predicates needed for Phase 5 is identified.
