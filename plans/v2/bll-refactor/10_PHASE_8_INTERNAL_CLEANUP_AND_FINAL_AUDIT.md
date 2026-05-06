# Phase 8 Agent Brief — Internal Cleanup and Final BLL Audit

Give this file to the Phase 8 agent together with `00_MASTER_BLL_AGENT_HANDOFF.md` and all prior phase reports.

---

## Goal

Remove obsolete BLL contracts/services/DTOs after the domain-first facade and BaseService-backed services are in place, then run the final architecture audit.

---

## Scope

In scope:

```text
App.BLL.Contracts cleanup
App.BLL.DTO cleanup
App.BLL service cleanup
AppBLL cleanup
using cleanup
obsolete service removal
final dependency audit
final build
trusted scope/context audit
```

Out of scope:

```text
WebApp mapper cleanup
WebApp controller migration unless explicitly assigned
API controllers
tests
DAL schema/repository redesign
```

---

## Tasks

1. Identify old granular contracts no longer used.
2. Remove old granular contracts from `App.BLL.Contracts` if safe.
3. Keep implementation helpers internal where practical.
4. Move helper services to internal/domain subfolders if useful.
5. Remove redundant DTOs and mappers.
6. Remove obsolete using statements.
7. Ensure `IAppBLL` exposes only intended domain services.
8. Ensure aggregate-backed exposed services inherit BaseService.
9. Ensure orchestration exceptions are documented.
10. Run build.
11. Produce final audit report.

---

## Final audit checklist

```text
IAppBLL exposes fewer domain-first services.
BaseService/IBaseService readiness was implemented and verified in Phase 2.
IBaseService no longer exposes public Add.
BaseService exposes protected AddCore for domain create wrappers.
Every aggregate-backed IAppBLL service inherits BaseService.
Any non-BaseService exposed service is documented as a pure orchestration exception.
AppBLL composes services cleanly.
App.BLL has no App.DAL.EF reference.
App.BLL.Contracts has no DAL DTO exposure.
All BLL service methods return Result/Result<T>, except documented SaveChangesAsync exception if retained.
Repositories do not return FluentResults.
Canonical BLL DTOs inherit BaseEntity.
Canonical BLL DTOs map to canonical DAL DTOs through IBaseMapper mappers.
Trivial CRUD commands/queries are removed or justified.
Canonical BLL DTOs are used as much as possible where they make sense.
Plain CRUD methods prefer returning canonical BLL DTOs.
Trusted route/scope models carry actor, tenant, route, parent-resource, and permission context separately from canonical DTOs.
Custom DTOs are not kept when they are only overengineered duplicates of canonical DTOs.
Workflow DTOs remain where justified.
Inherited BaseService CRUD methods are treated as mechanical primitives, not complete authorization-safe workflows.
Public create operations live on domain services as contextual route/scope + canonical DTO methods.
Each normal CRUD operation has one canonical repository-mutating method that prefers returning the canonical BLL DTO.
Projection-returning mutation methods compose canonical CRUD methods and do not duplicate repository-changing logic.
Workflow methods live inside domain services.
DeleteGuard remains in BLL.
ManagementCompany.DeleteCascadeAsync remains untouched.
BLL remains MVC/API/WebApp-neutral.
```

---

## Deliverable

Create:

```text
plans/bll-refactor/PHASE_8_FINAL_BLL_AUDIT.md
```

Include:

```text
build result
final IAppBLL surface
BaseService-backed service list
orchestration exception list
remaining TODOs
known WebApp caller breakages if any
files removed
files kept intentionally
```

---

## Acceptance criteria

```text
App.BLL.Contracts contains domain-first public BLL contracts.
Implementation can still be internally split.
No dead service contracts remain unless intentionally documented.
No dead DTOs remain unless intentionally documented.
IAppBLL exposes only intended domain services.
Build status documented.
```
