# DAL Refactor Phase 1: Stabilize base repository usage

> Scope file only. Use this together with `DAL_REFACTOR_MASTER_GUIDANCE.md`.
> Do not implement other phases unless explicitly instructed.

### Goals

- Ensure the base repository API is clear and stable.
- Avoid adding unnecessary `AsNoTracking()` because no-tracking is already configured globally.
- Ensure update methods that mutate loaded entities use `AsTracking()`.
- Avoid changing business behavior in this phase.

### Tasks

1. Review `Base.DAL.Contracts/IBaseRepository.cs`.
   - Confirm the scope parameter is consistently named `ParentId` or `parentId`.
   - Ensure method names and signatures are consistent.
   - Confirm cancellation token strategy. If adding cancellation tokens to base methods, update implementations and callers in a focused commit.

2. Review `Base.DAL.EF/BaseRepository.cs`.
   - Keep generic CRUD only.
   - Keep protected access to `RepositoryDbContext`, `RepositoryDbSet`, and mapper.
   - Do not add app-specific delete logic.
   - Do not add domain-specific joins.
   - Do not add `AsNoTracking()` unless intentionally overriding global behavior.

3. Audit current concrete repositories.
   - Mark which methods are generic CRUD and can use inherited base methods.
   - Mark which methods are projections/searches and should remain custom.
   - Mark which methods are cross-aggregate workflows and should move to BLL orchestration.

### Acceptance criteria

- Base repository still builds.
- No business workflow has been moved yet.
- A clear method classification exists for each repository.
