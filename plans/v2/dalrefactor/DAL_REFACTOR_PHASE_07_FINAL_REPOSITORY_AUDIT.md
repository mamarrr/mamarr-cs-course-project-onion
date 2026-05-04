# DAL Refactor Phase 7: Final Repository Audit

> Scope file only. Use this together with `DAL_REFACTOR_MASTER_GUIDANCE.md`.
> Do not implement other phases unless explicitly instructed.

## Goals

- Verify repository boundaries after the DAL refactor.
- Verify blocked-delete policy is consistently applied.
- Remove stale methods and duplicate semantics.
- Confirm the final architecture is understandable and maintainable.

## Checklist per repository

For every repository, verify:

- It inherits from `BaseRepository` where applicable.
- It uses the canonical DAL DTO for base CRUD.
- It does not duplicate base CRUD without reason.
- It does not query unrelated DbSets except for necessary parent-scope checks or projections.
- It does not orchestrate large cross-aggregate cascade deletes.
- It exposes existence predicates only for its own entity.
- It exposes dependency summary/check methods only for its own entity or natural aggregate.
- It uses clear method names.
- It uses `AsTracking()` for load-and-mutate update methods.
- It relies on global no-tracking for read methods unless explicit override is needed.
- It does not contain duplicated logic that exists in another repository.

## Checklist for delete behavior

Verify:

- Important business deletes are blocked when dependent records exist.
- Converted delete methods do not silently delete related business records.
- BLL delete guard returns dependency summaries or useful blocked-delete information.
- Repository delete methods delete only the requested entity, except for true owned child rows if explicitly allowed.
- FK constraints remain as a safety net.
- Raw FK constraint names are not exposed as normal user-facing messages.

## Checklist for BLL

Verify:

- Validation decisions are in BLL services.
- Cross-aggregate delete orchestration has not been reintroduced.
- Blocked-delete decisions are handled by BLL delete guard / policy.
- BLL uses repositories through `IAppUOW`.
- BLL does not use EF Core or `AppDbContext` directly.
- BLL does not depend on WebApp or ASP.NET Identity infrastructure.

## Checklist for contracts

Verify:

- Repository contracts expose persistence/query capabilities, not business workflows.
- BLL contracts expose business use cases.
- DTO namespaces are consistent with project boundaries.
- No obsolete methods remain in contracts.
- No old cascade-delete methods remain in repository contracts unless intentionally kept for true owned children.

## Duplicate semantic audit

Search for duplicate semantics across repositories:

- `ExistsInCompanyAsync`
- `ExistsInCustomerAsync`
- `ExistsInPropertyAsync`
- `HasDeleteDependenciesAsync`
- `GetDeleteDependencySummaryAsync`
- `SlugExists...`
- `RegistryCodeExists...`
- `AllSlugs...`
- `Search...`
- `List...Options...`
- `Delete...`

For each duplicate:

1. Confirm whether duplication is intentional.
2. If not intentional, keep it in the owning repository.
3. Update callers.
4. Remove stale contract methods.
5. Build.

## Out of scope

- Do not start a new architectural refactor.
- Do not convert every BLL service to `BaseService`.
- Do not rewrite controllers unless stale contract usage is discovered.
- Do not change database schema unless audit finds a real FK/cascade mismatch.

## Acceptance criteria

- Repositories are focused.
- Base CRUD is reused where practical.
- Delete behavior is blocked-delete by default.
- No repository contains large cross-aggregate cascade delete workflows.
- BLL delete guard is the central place for dependency-based delete decisions.
- The solution builds.
