# Phase 17 - Remembered Context Naming Subplan

Parent plan: `BLL_WEBAPP_AKAVER_REFACTOR_PLAN.md`

## Scope

Remove cookie-specific naming from BLL workspace redirect contracts while preserving current redirect behavior.

## Guardrails

- Do not change redirect behavior.
- Do not remove `SetContext` fallback in this phase.
- Do not change API controllers, tests, Base projects, DAL, schema, or migrations.
- Keep cookie read/write mechanics in WebApp, not BLL.

## Implementation Steps

1. Rename `WorkspaceRedirectCookieState` to a route-neutral remembered context model.
2. Rename `ResolveWorkspaceRedirectQuery.CookieState` to `RememberedContext`.
3. Update BLL service local variable names accordingly.
4. Update WebApp construction of the query.
5. Run source-only scans and ask the project owner to build.

## Acceptance Checks

- No BLL contract or service symbol uses `CookieState`/`Cookie` for workspace redirect input.
- WebApp still reads cookies and maps them into the route-neutral BLL remembered context.
- Redirect behavior remains unchanged.
- Build is delegated to the project owner.
