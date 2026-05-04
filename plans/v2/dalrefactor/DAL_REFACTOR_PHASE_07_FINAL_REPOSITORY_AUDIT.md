# DAL Refactor Phase 7: Final repository audit

> Scope file only. Use this together with `DAL_REFACTOR_MASTER_GUIDANCE.md`.
> Do not implement other phases unless explicitly instructed.

### Checklist per repository

For every repository, verify:

- It inherits from `BaseRepository` where applicable.
- It uses the canonical DAL DTO for base CRUD.
- It does not duplicate base CRUD without reason.
- It does not query unrelated DbSets except for necessary parent-scope checks or projections.
- It does not orchestrate large cross-aggregate workflows.
- It exposes existence predicates only for its own entity.
- It uses clear method names.
- It uses `AsTracking()` for load-and-mutate update methods.
- It relies on global no-tracking for read methods unless explicit override is needed.
- It does not contain duplicated logic that exists in another repository.

### Checklist for BLL

- Validation decisions are in BLL services.
- Cross-aggregate deletes are in a BLL delete orchestrator.
- BLL uses repositories through `IAppUOW`.
- BLL does not use EF Core or `AppDbContext` directly.
- BLL does not depend on WebApp or ASP.NET Identity infrastructure.

### Checklist for contracts

- Repository contracts expose persistence/query capabilities, not business workflows.
- BLL contracts expose business use cases.
- DTO namespaces are consistent with project boundaries.
- No obsolete methods remain in contracts.
