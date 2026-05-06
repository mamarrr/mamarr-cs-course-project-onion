# Phase 0 Agent Brief — Baseline and Guardrails

Give this file to the Phase 0 agent together with `00_MASTER_BLL_AGENT_HANDOFF.md`.

---

## Goal

Establish a safe baseline and document the current BLL state before refactoring.

Do not perform architecture refactoring in this phase.

---

## Scope

In scope:

```text
solution/build inspection
App.BLL.Contracts inspection
App.BLL.DTO inspection
App.BLL inspection
BaseService current behavior inspection
dependency audit
documentation/checklist creation
```

Out of scope:

```text
changing service contracts
changing DTOs
changing mappers
changing AppBLL
changing BaseService unless only documenting issues
changing DAL/WebApp/API/tests
```

---

## Tasks

1. Confirm the solution builds.
2. Confirm current `BaseService` compiles with `FluentResults` signatures.
3. Confirm `App.BLL` has no reference to `App.DAL.EF`.
4. Confirm `App.BLL.Contracts` does not expose DAL DTOs.
5. Record the current `IAppBLL` surface.
6. Record all currently exposed granular services.
7. Record which exposed services are likely aggregate-backed.
8. Record which exposed services are likely orchestration-only.
9. Record current BLL DTO folders and DTO categories at a high level.
10. Record all obvious permission-gate risks.

---

## Deliverable

Create or update a planning artifact such as:

```text
plans/bll-refactor/PHASE_0_BASELINE_REPORT.md
```

Suggested sections:

```text
Build status
Current IAppBLL surface
Current BLL service list
Current BLL DTO shape
BaseService current behavior
Dependency audit
Known risks
Recommended next-phase notes
```

---

## Acceptance criteria

```text
Build baseline known.
Current BLL service list documented.
Current IAppBLL surface documented.
BaseService adoption targets roughly identified.
Orchestration exceptions roughly identified.
No architecture refactor performed.
```

---

## Handoff to next agent

The next agent needs:

```text
build status
list of current granular services
list of current BLL DTOs/categories
list of likely aggregate-backed services
list of likely orchestration exceptions
any build/dependency problems discovered
```
