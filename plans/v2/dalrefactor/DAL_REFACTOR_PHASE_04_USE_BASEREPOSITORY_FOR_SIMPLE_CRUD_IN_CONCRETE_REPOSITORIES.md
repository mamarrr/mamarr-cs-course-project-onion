# DAL Refactor Phase 4: Use `BaseRepository` for simple CRUD in concrete repositories

> Scope file only. Use this together with `DAL_REFACTOR_MASTER_GUIDANCE.md`.
> Do not implement other phases unless explicitly instructed.

### Goals

- Remove duplicated simple CRUD from concrete repositories.
- Keep custom methods for projections, scoped reads, searches, and updates involving `LangStr` or complex field rules.

### Tasks per repository

For each repository:

1. Confirm it inherits from `BaseRepository<XxxDalDto, XxxDomainEntity, AppDbContext>`.
2. Confirm `IXxxRepository` inherits from `IBaseRepository<XxxDalDto>`.
3. Remove custom implementations that duplicate base `AllAsync`, `FindAsync`, `Add`, `Update`, or `RemoveAsync`, if behavior is identical.
4. Keep custom `AddAsync` methods when creation uses a separate create DTO or sets defaults such as generated ids, slugs, timestamps, active flags, or `LangStr` fields.
5. Keep custom `UpdateAsync` methods when update rules differ from simple full entity replacement.
6. Keep custom projection methods.
7. Keep custom query predicates that are owned by this repository.

### Recommended order

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

Do not start with `CustomerRepository`, `PropertyRepository`, `UnitRepository`, or `TicketRepository` delete workflows.

### Acceptance criteria

- Easy CRUD uses `BaseRepository` where possible.
- Concrete repositories contain only meaningful custom behavior.
- Build succeeds after each repository or small group of repositories.
