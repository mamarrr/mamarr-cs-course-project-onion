# Phase 2 Agent Brief — Stabilize BaseService Usage Rules

Give this file to the Phase 2 agent together with `00_MASTER_BLL_AGENT_HANDOFF.md`, Phase 0 report, and Phase 1 inventory.

---

## Goal

Make BaseService safe and clearly defined before it becomes the standard foundation for aggregate-backed domain services.

---

## Scope

In scope:

```text
Base.BLL/BaseService.cs inspection
Base.BLL.Contracts/IBaseService.cs inspection
documentation of BaseService usage rules
small BaseService cleanup proposal or implementation if approved
```

Out of scope unless approved:

```text
large Base project redesign
DAL repository redesign
changing UOW architecture
changing app services extensively
WebApp/API/tests
```

---

## Required decisions

Decide and document:

1. Should `BaseService.FindAsync` return `Result<T>` instead of `Result<T?>` when not-found is failure?
2. Should mapper null checks replace null-forgiving `!` in BaseService?
3. Should `Remove(entity)` remain in `IBaseService`, or should app services prefer `RemoveAsync(id, parentId)`?
4. Are generic BaseService string errors acceptable?
5. Should app services wrap/override BaseService generic errors with typed app errors where needed?
6. Which exposed services are aggregate-backed and must inherit BaseService?
7. Which exposed services are orchestration exceptions?

---

## Recommended BaseService cleanup

Preferred changes if approved:

```text
FindAsync returns Result<TBLLEntity> if not-found is failure.
Mapper null checks are added.
BaseService keeps generic errors, not app-specific errors.
App-specific services may wrap/override errors with typed app errors.
App services prefer RemoveAsync(id, parentId).
```

Do not make Base depend on App-specific errors like `NotFoundError`.

---

## Deliverable

Create or update:

```text
plans/bll-refactor/PHASE_2_BASESERVICE_DECISIONS.md
```

If code changes are approved, also update:

```text
Base.BLL/BaseService.cs
Base.BLL.Contracts/IBaseService.cs
```

---

## Acceptance criteria

```text
BaseService behavior is clear before broad adoption.
No repository returns FluentResults.
Every future IAppBLL service either inherits BaseService or has documented orchestration-exception status.
Any Base changes are small, explained, and buildable.
No app workflow logic moved into BaseService.
```

---

## Handoff to next agent

The next agent needs:

```text
final BaseService method signatures
final IBaseService signatures
approved BaseService usage rule
list of aggregate-backed service targets
list of orchestration exceptions
```
