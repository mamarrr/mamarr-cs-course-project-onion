# DAL Refactor Phase 1: Stabilize Base Repository Usage

> Scope file only. Use this together with `DAL_REFACTOR_MASTER_GUIDANCE.md`.
> Do not implement other phases unless explicitly instructed.

## Goals

- Ensure the base repository API is clear and stable.
- Avoid adding unnecessary `AsNoTracking()` because no-tracking is already configured globally.
- Ensure update methods that mutate loaded entities use `AsTracking()`.
- Avoid changing business behavior in this phase.
- Keep delete behavior unchanged for now; only prepare the base infrastructure.

## Tasks

1. Review `Base.DAL.Contracts/IBaseRepository.cs`.
   - Confirm the scope parameter is consistently named `ParentId` or `parentId`.
   - Ensure method names and signatures are consistent.
   - Confirm cancellation token strategy.
   - If adding cancellation tokens to base methods, update implementations and callers in a focused commit.

2. Review `Base.DAL.EF/BaseRepository.cs`.
   - Keep generic CRUD only.
   - Keep protected access to `RepositoryDbContext`, `RepositoryDbSet`, and mapper.
   - Do not add app-specific delete logic.
   - Do not add domain-specific joins.
   - Do not add cascade delete logic.
   - Do not add `AsNoTracking()` unless intentionally overriding global behavior.

3. Audit current concrete repositories.
   - Mark which methods are generic CRUD and can use inherited base methods.
   - Mark which methods are projections/searches and should remain custom.
   - Mark which methods are cross-aggregate delete workflows and should later be replaced by blocked-delete checks.
   - Do not move delete behavior yet.

## Out of scope

- Do not create delete guard services in this phase.
- Do not move repository methods between repositories yet.
- Do not change DTO shapes unless required by base repository compilation.
- Do not remove existing delete behavior yet.

## Acceptance criteria

- Base repository still builds.
- No business workflow has been moved yet.
- No delete behavior has been changed yet.
- A clear method classification exists for each repository.
