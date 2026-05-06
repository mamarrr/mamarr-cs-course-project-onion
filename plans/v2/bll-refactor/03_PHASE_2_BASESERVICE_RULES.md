# Phase 2 Agent Brief — BaseService / IBaseService Readiness

Give this file to the Phase 2 agent together with `00_MASTER_BLL_AGENT_HANDOFF.md`, Phase 0 report, and Phase 1 inventory.

---

## Goal

Make `BaseService` and `IBaseService` ready for the rest of the BLL refactor implementation.

This phase is an implementation phase. It must update the public base service surface so unsafe generic create is no longer exposed through `IBaseService`.

The rest of the plan assumes aggregate-backed domain services can safely inherit `BaseService` and use protected add helpers as their mechanical create foundation.

---

## Why this phase exists

Current `IBaseService` exposes:

```csharp
Result<TKey> Add(TEntity entity);
```

and current `BaseService` implements it as a public method.

That is not ideal for this app because generic create needs actor, tenant, route, permission, parent-resource, duplicate-check, and server-owned-field context.

Therefore:

```text
Public Add(entity) must be removed from IBaseService.
BaseService should keep protected AddCore(entity).
Domain services should expose contextual CreateAsync(route/scope, canonicalDto).
```

---

## Target IBaseService shape

Update `IBaseService` to remove public `Add`.

Target:

```csharp
public interface IBaseService<TEntity> : IBaseService<Guid, TEntity>
    where TEntity : IBaseEntity<Guid>
{
}

public interface IBaseService<TKey, TEntity>
    where TKey : IEquatable<TKey>
    where TEntity : IBaseEntity<TKey>
{
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

    Result Remove(TEntity entity);

    Task<Result> RemoveAsync(
        TKey id,
        TKey parentId = default!,
        CancellationToken cancellationToken = default);
}
```

Important:

```text
FindAsync returns Result<TEntity>, not Result<TEntity?>.
Not-found is a failed Result.
Public Add is not part of IBaseService.
```

---

## Target BaseService add helpers

Replace public `Add` with protected `AddCore`.

Target:

```csharp
protected virtual Result<TKey> AddCore(TBLLEntity entity)
{
    var mappedEntity = Mapper.Map(entity);

    if (mappedEntity is null)
    {
        return Result.Fail<TKey>(EntityMappingError);
    }

    return Result.Ok(ServiceRepository.Add(mappedEntity));
}
```

Optional but recommended helper:

```csharp
protected virtual async Task<Result<TBLLEntity>> AddAndFindCoreAsync(
    TBLLEntity entity,
    TKey parentId = default!,
    CancellationToken cancellationToken = default)
{
    var addResult = AddCore(entity);

    if (addResult.IsFailed)
    {
        return Result.Fail<TBLLEntity>(addResult.Errors);
    }

    await ServiceUOW.SaveChangesAsync(cancellationToken);

    return await FindAsync(addResult.Value, parentId, cancellationToken);
}
```

This helper is still mechanical. It does not resolve actor, tenant, route, permission, duplicate checks, or workflow.

If the team does not want BaseService to call `SaveChangesAsync`, skip `AddAndFindCoreAsync` and keep only `AddCore`. Then domain services call `ServiceUOW.SaveChangesAsync` themselves.

Preferred for this project:

```text
AddCore is required.
AddAndFindCoreAsync is optional but useful.
If added, clearly document that it is a mechanical helper and not a safe public create workflow.
```

---

## Required BaseService behavior

`BaseService` must:

```text
map DAL DTO collections to BLL DTO collections
return failed Result if collection item mapping fails
return failed Result if FindAsync cannot find entity
return failed Result if single entity mapping fails
check existence before UpdateAsync
check existence before RemoveAsync
return generic Base errors for not-found/mapping failure
not expose public Add
expose protected AddCore
optionally expose protected AddAndFindCoreAsync
not use App-specific typed errors
not throw for normal not-found/mapping failure
not contain authorization logic
not contain tenant/membership/role logic
not contain route slug resolution
not contain delete guard logic
not contain workflow/business rules
```

Expected generic Base errors:

```text
Entity not found.
Entity mapping failed.
```

Exact text may vary slightly, but keep them generic and Base-owned.

---

## Scope

In scope:

```text
Base.BLL.Contracts/IBaseService.cs
Base.BLL/BaseService.cs
small compile-safe BaseService cleanup
documentation of BaseService usage rules
verification that repositories still do not return FluentResults
verification that Base does not depend on App-specific errors
```

Out of scope unless explicitly approved:

```text
large Base project redesign
DAL repository redesign
UOW architecture redesign
changing SaveChangesAsync to Result<int>
moving authorization into BaseService
moving delete guard into BaseService
moving app-specific errors into Base
adding route/scope types to Base projects
WebApp/API/tests
```

---

## Readiness tasks

1. Remove `Result<TKey> Add(TEntity entity)` from `IBaseService`.
2. Remove public `Add(TBLLEntity entity)` from `BaseService`.
3. Add protected `AddCore(TBLLEntity entity)` to `BaseService`.
4. Optionally add protected `AddAndFindCoreAsync(...)` if approved.
5. Verify `IBaseService.FindAsync` returns `Task<Result<TEntity>>`, not nullable.
6. Verify `BaseService.FindAsync` returns failed Result when repository returns null.
7. Verify `BaseService.AllAsync` handles mapper nulls without `!`.
8. Verify `BaseService.UpdateAsync`:
   - checks existence first,
   - handles mapper nulls,
   - maps result safely.
9. Verify `BaseService.RemoveAsync` checks existence first.
10. Verify `BaseService.Remove(entity)` handles mapper nulls.
11. Verify BaseService does not call app-specific typed errors.
12. Verify BaseService does not authorize actor access.
13. Verify BaseService does not resolve route slugs.
14. Verify repositories still return plain DAL values/nulls/ids and not `FluentResults`.
15. Find any App.BLL services or tests currently calling public `Add`; update them to use domain-specific create wrappers or protected `AddCore` from inherited services where appropriate.
16. Document that app services should prefer contextual delete wrappers over raw `Remove(entity)`.

---

## Domain create method rule

Because `Add` is no longer public on `IBaseService`, every aggregate-backed domain service that supports create should define its own contextual create method.

Example:

```csharp
Task<Result<PropertyBllDto>> CreateAsync(
    CustomerRoute route,
    PropertyBllDto dto,
    CancellationToken cancellationToken = default);
```

The domain method should:

```text
resolve trusted scope
authorize actor
validate entity state
check duplicates/cross-tenant references
set server-owned fields such as parent id / slug / status
call AddCore(dto) or AddAndFindCoreAsync(dto, parentId, ct)
save/reload where needed
return typed app errors for expected failures
```

Do not add route/scope-aware create methods to BaseService. `CustomerRoute`, `PropertyRoute`, `CompanyScope`, etc. are App.BLL concepts and must not enter Base projects.

---

## Important design decision: inherited CRUD is mechanical

Because aggregate-backed contracts may inherit `IBaseService<TBllDto>`, callers may technically see inherited methods like:

```text
FindAsync(id, parentId)
UpdateAsync(dto, parentId)
RemoveAsync(id, parentId)
```

These are mechanical CRUD primitives.

They are not complete safe public app workflows when actor/tenant authorization is required.

Domain contracts should expose safe contextual wrappers where needed:

```csharp
Task<Result<PropertyBllDto>> CreateAsync(
    CustomerRoute route,
    PropertyBllDto dto,
    CancellationToken cancellationToken = default);

Task<Result<PropertyBllDto>> UpdateAsync(
    PropertyRoute route,
    PropertyBllDto dto,
    CancellationToken cancellationToken = default);
```

The wrapper resolves route/natural keys into trusted scope before calling BaseService.

---

## Deliverable

Create or update:

```text
plans/bll-refactor/PHASE_2_BASESERVICE_READINESS_REPORT.md
```

The report must include:

```text
current IBaseService signatures
current BaseService behavior
readiness checklist result
code changes made
confirmation public Add was removed
confirmation protected AddCore was added
confirmation whether AddAndFindCoreAsync was added
repository FluentResults audit result
known limitations
handoff notes for Phase 3 / Phase 3.5 / Phase 4 agents
```

---

## Acceptance criteria

```text
IBaseService no longer exposes public Add.
BaseService no longer exposes public Add.
BaseService exposes protected AddCore.
Optional AddAndFindCoreAsync is either implemented or explicitly deferred.
IBaseService FindAsync is non-nullable Result<TEntity>.
BaseService returns failed Results for not-found and mapping failures.
BaseService has no App-specific typed errors.
BaseService has no authorization/tenant/route/workflow logic.
Repositories do not return FluentResults.
BaseService is documented as mechanical CRUD only.
Domain services are instructed to expose safe route/scope create wrappers.
Any code changes are small, explained, and buildable.
```

---

## Handoff to next agent

The next agent needs:

```text
final BaseService method signatures
final IBaseService method signatures
confirmation that public Add was removed
protected AddCore signature
AddAndFindCoreAsync status
confirmation that BaseService is ready for inheritance
confirmation that create belongs to domain services as route/scope + canonical DTO
list of aggregate-backed service targets
list of orchestration exceptions
any remaining BaseService limitations
```
