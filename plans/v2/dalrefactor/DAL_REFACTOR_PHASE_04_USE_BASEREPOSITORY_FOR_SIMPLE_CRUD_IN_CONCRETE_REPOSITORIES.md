# DAL Refactor Phase 4: Use BaseRepository for Simple CRUD in Concrete Repositories

> Scope file only. Use this together with `DAL_REFACTOR_MASTER_GUIDANCE.md`.
> Do not implement other phases unless explicitly instructed.

## Goals

- Remove duplicated simple CRUD from concrete repositories.
- Keep custom methods for projections, scoped reads, searches, and updates involving `LangStr` or complex field rules.
- Keep current delete behavior unchanged unless replacing a simple delete with a base delete is clearly safe.
- Do not implement blocked-delete policy yet.

## Tasks per repository

For each repository:

1. Confirm it inherits from `BaseRepository<XxxDalDto, XxxDomainEntity, AppDbContext>`.

2. Confirm `IXxxRepository` inherits from `IBaseRepository<XxxDalDto>`.

3. Remove custom implementations that duplicate base methods, if behavior is identical:
   - `AllAsync`
   - `FindAsync`
   - `Add`
   - `Update`
   - `Remove`
   - `RemoveAsync`

4. Keep custom `AddAsync` methods when creation uses a separate create DTO or sets defaults such as:
   - generated ids
   - slugs
   - timestamps
   - active flags
   - `LangStr` fields

5. Keep custom `UpdateAsync` methods when update rules differ from simple full entity replacement.

6. Keep custom projection methods.

7. Keep custom query predicates that are owned by this repository.

8. Keep complex delete methods temporarily. They will be replaced by blocked-delete validation in Phase 5/6, not during this phase.

## Recommended order

Start with simpler repositories:

1. `ContactRepository`
2. `ManagementCompanyJoinRequestRepository`
3. `LookupRepository`
4. `UnitRepository`
5. `PropertyRepository`
6. `ResidentRepository`
7. `CustomerRepository`
8. `LeaseRepository`
9. `TicketRepository`

Do not start with complex delete workflows in:

- `CustomerRepository`
- `PropertyRepository`
- `UnitRepository`
- `TicketRepository`

## Out of scope

- Do not create BLL delete guard.
- Do not replace cascade deletes yet.
- Do not move projection/search methods unless they clearly duplicate Phase 3 ownership cleanup.
- Do not convert BLL services to `BaseService`.

## Acceptance criteria

- Easy CRUD uses `BaseRepository` where possible.
- Concrete repositories contain only meaningful custom behavior.
- Build succeeds after each repository or small group of repositories.
- Complex delete behavior is not accidentally changed.
