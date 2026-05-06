# Phase 2 BaseService Readiness Report

Source brief: `plans/v2/bll-refactor/03_PHASE_2_BASESERVICE_RULES.md`  
Master handoff: `plans/v2/bll-refactor/00_MASTER_BLL_AGENT_HANDOFF.md`  
Phase 0 report: `plans/v2/bll-refactor/phase-reports/PHASE_0_BASELINE_REPORT.md`  
Phase 1 inventory: `plans/v2/bll-refactor/phase-reports/PHASE_1_BLL_INVENTORY.md`  
Date: 2026-05-06

## Code Changes Made

- Removed public generic `Add(TEntity entity)` from `IBaseService<TKey, TEntity>`.
- Removed public generic `Remove(TEntity entity)` from `IBaseService<TKey, TEntity>`.
- Replaced public `BaseService.Add(...)` with protected `AddCore(...)`.
- Removed public `BaseService.Remove(entity)` so unchecked entity-based deletes are no longer exposed.
- Added protected `AddAndFindCoreAsync(...)` as a mechanical helper that calls `AddCore`, saves through the UOW, and reloads through `FindAsync`.

Changed files:

- `Base.BLL.Contracts/IBaseService.cs`
- `Base.BLL/BaseService.cs`

## Current IBaseService Signatures

```csharp
Task<Result<IEnumerable<TEntity>>> AllAsync(
    TKey parentId = default!,
    CancellationToken cancellationToken = default);

Task<Result<TEntity>> FindAsync(
    TKey id,
    TKey parentId = default!,
    CancellationToken cancellationToken = default);

Task<Result<TEntity>> UpdateAsync(
    TEntity entity,
    TKey parentId = default!,
    CancellationToken cancellationToken = default);

Task<Result> RemoveAsync(
    TKey id,
    TKey parentId = default!,
    CancellationToken cancellationToken = default);
```

## Current BaseService Protected Create Helpers

```csharp
protected virtual Result<TKey> AddCore(TBLLEntity entity)
```

```csharp
protected virtual async Task<Result<TBLLEntity>> AddAndFindCoreAsync(
    TBLLEntity entity,
    TKey parentId = default!,
    CancellationToken cancellationToken = default)
```

`AddAndFindCoreAsync` is intentionally mechanical only. It does not resolve actor identity, tenant scope, route slugs, permissions, duplicate checks, delete guards, or workflow rules. Domain services must expose safe route/scope create wrappers before calling it.

## Readiness Checklist

| Check | Result |
|---|---|
| `IBaseService` no longer exposes public `Add` | Pass |
| `BaseService` no longer exposes public `Add` | Pass |
| `BaseService` exposes protected `AddCore` | Pass |
| `AddAndFindCoreAsync` status | Added |
| `IBaseService.FindAsync` returns `Task<Result<TEntity>>`, not nullable | Pass |
| `BaseService.FindAsync` fails when repository returns null | Pass |
| `BaseService.AllAsync` fails on mapper nulls | Pass |
| `BaseService.UpdateAsync` checks existence before update | Pass |
| `BaseService.UpdateAsync` handles mapper nulls | Pass |
| `BaseService.RemoveAsync` checks existence before delete | Pass |
| Public unchecked `Remove(entity)` no longer exposed | Pass |
| BaseService uses generic base errors only | Pass |
| BaseService has no app-specific typed errors | Pass |
| BaseService has no authorization, tenant, route, delete guard, or workflow logic | Pass |
| Repository contracts return plain DAL values/nulls/ids, not `FluentResults` | Pass |

## Verification

Static checks performed:

- Searched `Base.BLL`, `Base.BLL.Contracts`, `App.BLL`, and `App.BLL.Contracts` for public `Add`, public `Remove(entity)`, `AddCore`, and `AddAndFindCoreAsync`.
- Searched `Base.DAL.Contracts` and `App.DAL.Contracts` for `FluentResults` and `Result` usage; no repository-contract `FluentResults` usage was found.
- Searched `Base.BLL` and `Base.BLL.Contracts` for app-specific typed errors and Web/MVC concepts; none were found.

Build was not run by the agent because the master handoff says builds should be run manually by the project owner.

Suggested manual command:

```powershell
dotnet build mamarrproject.sln -nologo
```

## Known Limitations

- No aggregate-backed app services inherit `BaseService` yet; this phase only prepares the base layer.
- `AddAndFindCoreAsync` calls `SaveChangesAsync`. Domain services that need multiple mutations in one transaction can call `AddCore` directly and manage saving themselves.
- Inherited `FindAsync`, `UpdateAsync`, and `RemoveAsync` remain mechanical CRUD primitives. Tenant-safe application workflows still need contextual wrappers in concrete domain services.
- Existing app services still use repository `Add` directly. That is unchanged and should be addressed when those services are migrated to domain-first BaseService-derived services.

## Handoff Notes

Next phases can now introduce aggregate-backed services that inherit `BaseService`, starting with:

- `CustomerService`
- `PropertyService`
- `UnitService`
- `ResidentService`
- `LeaseService`
- `TicketService`
- `ManagementCompanyService`

Create operations should be explicit domain methods shaped around route/scope plus canonical BLL DTOs. BaseService must remain mechanical and must not receive app-specific scope, authorization, or workflow behavior.
