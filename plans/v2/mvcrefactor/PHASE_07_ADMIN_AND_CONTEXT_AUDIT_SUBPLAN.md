# Phase 07: Admin Exception and Context Audit

Goal: close small boundary cleanup items before protected-area consolidation.

Scope:
- Add a colocated `WebApp/Areas/Admin/README.md` documenting the temporary
  Admin scaffolded EF exception and its guardrails.
- Replace the remaining protected-controller claim parsing in
  `Resident/UnitsController` with `ICurrentPortalContextResolver`.
- Remove stale protected-controller claim imports surfaced by the audit scan.
- Run source-only scans for BLL dependencies, DAL DTO leaks, non-Admin EF/UOW
  usage, and direct individual BLL service injections.

Out of scope:
- No route moves.
- No Portal area consolidation.
- No API changes.
- No DAL, schema, migration, or Base changes.
- No tests under the current testing override.
