# DAL Refactor Phase 4 — Use BaseRepository for Canonical CRUD in Vertical Slices

> Scope file only. Use this together with `DAL_REFACTOR_MASTER_GUIDANCE.md`.
> Do not implement other phases unless explicitly instructed.

## Purpose

Phase 4 standardizes simple repository CRUD around `BaseRepository`.

The goal is not just to remove duplicated code. The goal is to make every repository follow the same rule:

```text
Simple CRUD uses BaseRepository.
Concrete repositories contain only meaningful custom behavior.
Canonical CRUD methods use the canonical DAL DTO.
Special create/update/delete DTOs are not used for canonical CRUD.
```

This phase should be implemented by vertical slices, one repository/entity at a time.

---

# Context from earlier phases

Phase 1 classified which repository methods are generic CRUD candidates, projection/search methods, validation/existence methods, and cross-aggregate delete workflows.

Phase 3 already moved many ownership-mismatched methods to their correct repositories.

Phase 4 now focuses on method shape, canonical DTO shape, and reuse of `BaseRepository`.

Use this file together with:

- `DAL_REFACTOR_MASTER_GUIDANCE.md`
- `DAL_REFACTOR_REPOSITORY_METHOD_CLASSIFICATION.md`

---

# Important current decision

Canonical DAL DTOs are now the DTOs used for simple CRUD.

For each repository:

```text
Add       -> canonical TDalEntity
All       -> canonical TDalEntity collection
Find      -> canonical TDalEntity?
Remove    -> id / optional parentId
Update    -> canonical TDalEntity, through UpdateAsync when generic update is safe
```

Do not keep separate CRUD-only DTOs such as:

```text
XxxCreateDalDto
XxxUpdateDalDto
```

when they are only duplicates of the canonical DTO without metadata.

If a DTO exists only because older code had metadata fields like `CreatedAt`, and that metadata is now handled by domain/DAL infrastructure, remove the specialized DTO and use the canonical DTO.

The main point is **standardization**:

```text
Use the canonical DAL DTO for simple CRUD.
Use BaseRepository methods for simple CRUD.
Keep custom repository methods only for meaningful custom behavior.
```

---

# BaseRepository stability rule

`BaseRepository` should now be treated as stable during this phase.

Only change `BaseRepository` if there is a truly breaking bug.

Examples of breaking bugs:

- `Add` mutates one mapped domain object but adds another mapped object.
- `CreatedAt` support is not actually applied to the entity being added.
- `UpdateAsync` fails to preserve infrastructure metadata that the base method is expected to preserve.
- A base method violates its own contract.

Do not add app-specific behavior to `BaseRepository`.

Do not add delete orchestration to `BaseRepository`.

Do not add projection/query behavior to `BaseRepository`.

Do not change `BaseRepository` just to make one complex repository method fit.

## Current implementation assumption

The current base repository is expected to handle `CreatedAt` as infrastructure metadata:

```text
Add:
  sets CreatedAt for entities implementing IHasCreatedAtMeta.

UpdateAsync:
  preserves existing CreatedAt for entities implementing IHasCreatedAtMeta.
```

This is an implementation detail that supports the main Phase 4 rule: canonical CRUD DTOs should not need to carry metadata just to make persistence work.

Do not reintroduce `CreatedAt` into canonical DTOs just to support create/update.

---

# Canonical DAL DTO rule

Each aggregate/entity repository should have one canonical DAL DTO:

```text
ContactDalDto
CustomerDalDto
PropertyDalDto
UnitDalDto
ResidentDalDto
LeaseDalDto
TicketDalDto
VendorDalDto
ManagementCompanyDalDto
ManagementCompanyJoinRequestDalDto
```

The canonical DAL DTO should:

- represent the entity's simple scalar persistence shape;
- be safe for base CRUD;
- avoid navigation-derived display data;
- avoid `LangStr` as a property type;
- expose localized fields as `string` / `string?`;
- avoid metadata such as `CreatedAt` unless it is explicitly needed for a read model;
- inherit/use the project base entity shape so an `Id` exists on initialization;
- be the input/output type for simple base CRUD.

The canonical DTO should not include:

- `CreatedAt`, unless there is a specific read/display reason;
- `IsActive`, because it was removed from the domain;
- navigation DTOs;
- cross-entity display names;
- BLL-only validation state;
- Web/API view model fields.

Projection DTOs are still allowed and should remain separate.

Examples:

```text
UnitDalDto
  Id
  PropertyId
  UnitNr
  Slug
  FloorNr
  SizeM2
  Notes

UnitProfileDalDto
  Id
  PropertyId
  CustomerId
  ManagementCompanyId
  CompanySlug
  CustomerSlug
  PropertySlug
  UnitNr
  Slug
  FloorNr
  SizeM2
  Notes
  CreatedAt, only if displayed
```

---

# Method shape rule

For simple CRUD, repository contracts should rely on inherited base methods.

## Add

Use base method:

```text
Add(TDalEntity entity)
```

Do not create a custom repository method like:

```text
AddAsync(XxxCreateDalDto dto)
```

unless creation is not simple CRUD.

If creation only needs:

- `Id`
- `CreatedAt`
- scalar properties
- `LangStr` conversion from string

then it should use the canonical DTO and base `Add`.

The mapper may convert string fields into `LangStr` when creating a new domain entity.

`CreatedAt` is infrastructure/domain metadata and should not be supplied by create DTOs.

## All

Use base method:

```text
AllAsync(parentId, cancellationToken)
```

when the result is the canonical DTO collection and the base parent-scope filtering is sufficient.

Keep custom list methods when they return projection DTOs or require special ordering/filtering.

## Find

Use base method:

```text
FindAsync(id, parentId, cancellationToken)
```

when the result is the canonical DTO and base parent-scope filtering is sufficient.

Keep custom find/detail/profile/workspace methods when they return projection DTOs or require joins.

## Remove

Use base method:

```text
RemoveAsync(id, parentId, cancellationToken)
```

when deleting only the entity itself is the desired behavior.

Keep old complex delete methods temporarily only when they still represent the current behavior before Phase 5.

Do not implement blocked-delete policy in Phase 4.

Do not convert complex cascade-style deletes to base remove unless the current intended behavior is truly simple entity delete.

## Update

Use base method:

```text
UpdateAsync(TDalEntity entity)
```

when generic base update is safe.

Generic base update is safe when:

- the entity has no direct `LangStr` properties;
- the update shape is the same as the canonical DTO;
- the canonical DTO represents the full editable scalar state;
- omitted infrastructure metadata is handled by `BaseRepository`;
- no special business update rules are needed.

`CreatedAt` by itself is not a reason to keep a custom update if `BaseRepository.UpdateAsync` preserves it.

If the domain entity contains any direct `LangStr` property, do not use generic base update.

For `LangStr` entities, keep a custom repository update method, but it should still accept the canonical DTO where possible:

```text
UpdateAsync(TDalEntity dto, cancellationToken)
```

The custom update method should:

- load the existing domain entity with `AsTracking()`;
- update scalar editable fields;
- update existing `LangStr` values using `SetTranslation(...)`;
- avoid replacing the whole localized value object unless intentionally creating a new one;
- return the canonical DTO if a return value is needed, otherwise return a boolean/result according to existing project convention.

Do not keep `XxxUpdateDalDto` if it is just the same as `XxxDalDto`.

Keep a specialized update DTO only when the use-case shape is meaningfully different from the canonical DTO.

## Base UpdateAsync scope note

Current base update uses the entity id. If an update requires explicit management-company/customer/property scope checks, either:

- validate scope in BLL before calling base `UpdateAsync`, or
- keep a custom scoped repository update method.

Do not remove scoped custom update methods if the scope itself is meaningful business/access behavior.

---

# Build expectation for this phase

The DAL layer should be internally consistent after each vertical slice.

The following should compile after each slice where possible:

- `App.DAL.DTO`
- `App.DAL.Contracts`
- `App.DAL.EF`
- base projects used by DAL

A full solution build is preferred, but it is acceptable if the solution does not build only because BLL services, BLL mappers, or WebApp callers still reference old DAL create/update DTOs or old CRUD method names.

This is acceptable because a later refactor will introduce canonical BLL DTOs and `BaseService`-style service cleanup.

When leaving temporary BLL compile errors, document them clearly.

Do not leave DAL contract/implementation mismatches.

---

# Out of scope

Do not implement BLL canonical DTOs.

Do not convert BLL services to `BaseService`.

Do not refactor WebApp controllers.

Do not implement Phase 5 delete guard.

Do not replace cascade deletes with blocked-delete behavior.

Do not move repository ownership responsibilities unless a method clearly still violates Phase 3 and blocks this slice.

Do not change database schema unless a real model/migration bug is found.

Do not change `BaseRepository` except for a true breaking bug.

---

# Vertical slice implementation approach

Each vertical slice should handle one repository/entity completely.

For each slice:

1. Update the canonical DAL DTO.
2. Remove metadata from the canonical DAL DTO unless it is required for a read/display use case.
3. Remove redundant `CreateDalDto` / `UpdateDalDto` if they duplicate the canonical DTO.
4. Update the DAL mapper.
5. Update the repository contract.
6. Update the repository implementation.
7. Replace custom CRUD with inherited base methods where safe.
8. Use base `UpdateAsync` when the entity has no direct `LangStr` and generic update is safe.
9. Keep custom update if the domain entity has `LangStr`, scoped update behavior, workflow behavior, or other protected-field behavior.
10. Keep custom projection/search/option/profile/dashboard methods.
11. Keep complex delete behavior unchanged unless simple base remove is clearly correct.
12. Update DAL callers where practical.
13. Record any BLL/Web compile errors left for later service refactor work.

---

# Slice 1 — ContactRepository

## Target

`Contact` is the first pilot slice.

Expected canonical DTO:

```text
ContactDalDto
  Id
  ManagementCompanyId
  ContactTypeId
  ContactValue
  Notes
```

Remove from canonical DTO:

```text
CreatedAt
```

Remove if redundant:

```text
ContactCreateDalDto
ContactUpdateDalDto
```

## BaseRepository usage

Use inherited/base:

```text
Add(ContactDalDto)
FindAsync(id, parentId?, cancellationToken)
AllAsync(parentId?, cancellationToken)
RemoveAsync(id, parentId?, cancellationToken)
```

Keep custom:

```text
UpdateAsync(ContactDalDto dto, CancellationToken cancellationToken)
```

Reason: `Contact` has `LangStr? Notes`.

## Acceptance criteria

- `IContactRepository` inherits `IBaseRepository<ContactDalDto>`.
- No custom add method remains if base add is sufficient.
- Custom update accepts `ContactDalDto`, not `ContactUpdateDalDto`, unless a real shape difference is documented.
- Mapper no longer maps `CreatedAt` from DTO to domain.
- DAL compiles for this slice.

---

# Slice 2 — LeaseRepository

## Target

`LeaseDalDto` is already close to the target because it does not carry `CreatedAt`.

Expected canonical DTO:

```text
LeaseDalDto
  Id
  UnitId
  ResidentId
  LeaseRoleId
  StartDate
  EndDate
  Notes
```

Remove if redundant:

```text
LeaseCreateDalDto
```

Consider whether `LeaseUpdateDalDto` is still needed. If it differs only by naming `LeaseId` instead of `Id`, prefer canonical `LeaseDalDto`.

## BaseRepository usage

Use inherited/base:

```text
Add(LeaseDalDto)
FindAsync(id, parentId?, cancellationToken), only if useful
AllAsync(parentId?, cancellationToken), only if useful
RemoveAsync(id, parentId?, cancellationToken), only if unscoped/simple delete is appropriate
```

Keep custom:

```text
UpdateForResidentAsync(..., LeaseDalDto dto, ...)
UpdateForUnitAsync(..., LeaseDalDto dto, ...)
DeleteForResidentAsync(...)
DeleteForUnitAsync(...)
AllByResidentAsync(...)
AllByUnitAsync(...)
FirstByIdForResidentAsync(...)
FirstByIdForUnitAsync(...)
HasOverlappingActiveLeaseAsync(...)
```

Reason: lease update has scoped resident/unit workflows and `LangStr? Notes`.

## Acceptance criteria

- Canonical lease CRUD shape uses `LeaseDalDto`.
- Redundant create/update DTOs are removed if no longer needed.
- Scoped custom methods keep their scope parameters.
- `LangStr` update remains custom.
- Projection methods stay custom.

---

# Slice 3 — UnitRepository

## Target

Expected canonical DTO:

```text
UnitDalDto
  Id
  PropertyId
  UnitNr
  Slug
  FloorNr
  SizeM2
  Notes
```

Remove from canonical DTO:

```text
CreatedAt
```

Remove if redundant:

```text
UnitCreateDalDto
UnitUpdateDalDto
```

## BaseRepository usage

Use inherited/base:

```text
Add(UnitDalDto)
FindAsync(id, parentId?, cancellationToken), if useful
AllAsync(parentId?, cancellationToken), if useful
```

Keep custom:

```text
UpdateAsync(UnitDalDto dto, CancellationToken cancellationToken)
```

Reason: `Unit` has `LangStr? Notes`.

Be careful with delete:

```text
DeleteAsync(unitId, propertyId, managementCompanyId, cancellationToken)
```

currently has complex behavior and should remain unchanged until Phase 5 unless explicitly replaced by blocked delete.

## Acceptance criteria

- Canonical `UnitDalDto` has no metadata.
- Custom add is removed if base add is sufficient.
- Custom update accepts canonical DTO if no special update DTO is needed.
- Existing projection/search methods stay custom.
- Complex delete behavior is not changed in Phase 4.

---

# Slice 4 — ResidentRepository

## Target

Expected canonical DTO:

```text
ResidentDalDto
  Id
  ManagementCompanyId
  FirstName
  LastName
  IdCode
  PreferredLanguage
```

Remove from canonical DTO:

```text
CreatedAt
```

Remove if redundant:

```text
ResidentCreateDalDto
ResidentUpdateDalDto
```

## BaseRepository usage

Use inherited/base if safe:

```text
Add(ResidentDalDto)
UpdateAsync(ResidentDalDto)
FindAsync(id, parentId?, cancellationToken)
AllAsync(parentId?, cancellationToken)
```

Since `Resident` has no direct `LangStr` fields, generic base `UpdateAsync` may be safe if the canonical DTO represents exactly the editable scalar fields and BLL/custom logic handles required scope/access checks.

Keep custom projection/access methods:

```text
FirstProfileAsync
FindProfileAsync
AllByCompanyAsync
FirstActiveUserResidentContextAsync
HasActiveUserResidentContextAsync
ContactsByResidentAsync
LeaseSummariesByResidentAsync
OptionsForTicketAsync
SearchForLeaseAssignmentAsync
ExistsInCompanyAsync
IsLinkedToUnitAsync
IdCodeExistsForCompanyAsync
```

Keep complex delete unchanged until Phase 5.

## Acceptance criteria

- Resident simple CRUD uses base methods where parent scope behavior is sufficient.
- Resident update may use base `UpdateAsync` because `CreatedAt` is preserved by base infrastructure.
- Projection/access/search methods remain custom.
- Complex delete is not changed.

---

# Slice 5 — PropertyRepository

## Target

Expected canonical DTO:

```text
PropertyDalDto
  Id
  CustomerId
  PropertyTypeId
  Label
  Slug
  AddressLine
  City
  PostalCode
  Notes
```

Remove from canonical DTO:

```text
CreatedAt
```

Remove if redundant:

```text
PropertyCreateDalDto
PropertyUpdateDalDto
```

## BaseRepository usage

Use inherited/base:

```text
Add(PropertyDalDto)
FindAsync(id, parentId?, cancellationToken), if useful
AllAsync(parentId?, cancellationToken), if useful
```

Keep custom:

```text
UpdateProfileAsync(PropertyDalDto dto, ...)
```

or equivalent custom update with canonical DTO.

Reason: `Property` has `LangStr Label` and `LangStr? Notes`.

Keep projection/search/option methods.

Keep complex delete unchanged until Phase 5.

---

# Slice 6 — CustomerRepository

## Target

Expected canonical DTO:

```text
CustomerDalDto
  Id
  ManagementCompanyId
  Name
  Slug
  RegistryCode
  BillingEmail
  BillingAddress
  Phone
  Notes
```

Remove from canonical DTO:

```text
CreatedAt
```

Remove if redundant:

```text
CustomerCreateDalDto
CustomerUpdateDalDto
```

## BaseRepository usage

Use inherited/base:

```text
Add(CustomerDalDto)
FindAsync(id, parentId?, cancellationToken), if useful
AllAsync(parentId?, cancellationToken), if useful
```

Keep custom:

```text
UpdateProfileAsync(CustomerDalDto dto, ...)
```

or equivalent custom update with canonical DTO.

Reason: `Customer` has `LangStr? Notes`.

Keep custom projection/search/access methods.

Keep complex delete unchanged until Phase 5.

---

# Slice 7 — ManagementCompanyRepository

## Target

Expected canonical DTO should represent only the management company entity itself.

Remove metadata from canonical DTO unless needed for read display.

Do not include membership admin models, role models, join-request models, or user display data in the canonical DTO.

## BaseRepository usage

Use inherited/base for simple management-company entity CRUD if the repository inherits `BaseRepository`.

Keep custom:

- workspace/profile queries;
- membership queries and mutations;
- ownership/role logic;
- slug/registry checks;
- member projections;
- complex cascade delete until Phase 5.

Management company has no direct `LangStr` fields in the current domain shape, so generic `UpdateAsync` may be safe only if the canonical DTO exactly represents editable scalar profile fields and no workflow/protected fields would be overwritten.

---

# Slice 8 — ManagementCompanyJoinRequestRepository

## Target

Use canonical DTO for simple join-request persistence if possible.

Remove redundant create DTO only if the canonical DTO can safely represent the create shape.

Keep status transition methods custom if they represent workflow behavior.

## BaseRepository usage

Potential base methods:

```text
Add(canonical dto)
FindAsync(...)
AllAsync(...)
UpdateAsync(canonical dto), only for simple row edits
RemoveAsync(...)
```

Keep custom:

```text
SetStatusAsync
HasPendingRequestAsync
PendingByCompanyAsync
FindByIdAndCompanyAsync
```

Reason: these are workflow/query methods, not simple CRUD.

---

# Slice 9 — VendorRepository

## Target

Expected canonical DTO:

```text
VendorDalDto
  Id
  ManagementCompanyId
  Name
  RegistryCode
  Notes
```

Remove from canonical DTO:

```text
CreatedAt
```

## BaseRepository usage

Use inherited/base for simple add/find/all/remove if available.

Keep custom update if vendor has `LangStr Notes`.

Keep ticket option and existence methods custom.

---

# Slice 10 — TicketRepository

## Target

Ticket is complex. Do it last.

Canonical `TicketDalDto` should represent only the ticket entity itself.

It should not contain:

- status label;
- priority label;
- category label;
- customer/property/unit/resident/vendor display names;
- option lists;
- validation result data.

Those belong in projection DTOs.

## BaseRepository usage

Use base add/update only if safe, but likely keep custom methods because `Ticket` has localized fields, ticket number generation, status workflows, and validation rules.

Likely keep custom:

```text
AddAsync(...)
UpdateAsync(...)
UpdateStatusAsync(...)
GetNextTicketNrAsync(...)
TicketNrExistsAsync(...)
AllByCompanyAsync(...)
FindDetailsAsync(...)
FindForEditAsync(...)
DeleteAsync(...), until Phase 5
```

If create/update DTOs are still needed because ticket create/update shape is meaningfully different from the canonical entity shape, keep them and document why.

Do not force `TicketRepository` into base CRUD just for consistency.

---

# Repository methods that must remain custom

Keep custom methods for:

- projections;
- search;
- options/dropdowns;
- workspace/dashboard/profile reads;
- slug uniqueness checks;
- registry/id-code uniqueness checks;
- access/context queries;
- membership workflows;
- status transitions;
- scoped update/delete methods;
- dependency checks;
- `LangStr` updates;
- complex deletes until Phase 5.

---

# Repository methods that should be removed when redundant

Remove custom methods when they duplicate base behavior:

```text
FindAsync(id) returning canonical DTO
AllAsync(parentId) returning canonical DTO collection
AddAsync(CreateDto) where CreateDto is now same as canonical DTO
UpdateAsync(UpdateDto) where UpdateDto is now same as canonical DTO and generic base UpdateAsync is safe
DeleteAsync(id) where it only deletes the entity itself and base RemoveAsync is sufficient
```

If a method has different scope parameters or business behavior, do not remove it blindly.

---

# Acceptance criteria

Phase 4 is complete when:

- repositories inherit `BaseRepository<TDalEntity, TDomainEntity, AppDbContext>` where appropriate;
- repository contracts inherit `IBaseRepository<TDalEntity>` where appropriate;
- simple CRUD uses base methods;
- simple CRUD methods use canonical DAL DTOs;
- redundant create/update DTOs are removed where they duplicate canonical DTOs;
- canonical DTOs do not carry metadata unless required for read display;
- base `UpdateAsync` is used for simple non-`LangStr` entities where generic update is safe;
- custom updates remain for entities containing `LangStr`, scoped workflows, or protected-field behavior not handled by base update;
- projection/search/workflow methods remain custom;
- complex delete behavior remains unchanged until Phase 5;
- DAL contracts and DAL implementations are internally consistent;
- any remaining BLL/Web compile errors are documented as out-of-scope for this phase.

---

# Suggested execution order

Use vertical slices in this order:

1. `ContactRepository`
2. `LeaseRepository`
3. `UnitRepository`
4. `ResidentRepository`
5. `PropertyRepository`
6. `CustomerRepository`
7. `ManagementCompanyJoinRequestRepository`
8. `ManagementCompanyRepository`
9. `VendorRepository`
10. `TicketRepository`

After each slice:

```text
1. Build DAL projects if possible.
2. Fix DAL contract/implementation errors.
3. Record BLL/Web errors caused by old DTO names or method names.
4. Commit or checkpoint before moving to the next slice.
```

---

# Final reminder

This phase prepares DAL for the later BLL refactor.

Later, BLL may have its own canonical DTOs/models inheriting the same base entity pattern and may use `BaseService`.

That is not part of this phase.

During this phase, do not redesign BLL service contracts. Only update BLL references when necessary or convenient, and do not treat BLL mapper errors as blockers if they are caused by planned later DTO/service cleanup.
