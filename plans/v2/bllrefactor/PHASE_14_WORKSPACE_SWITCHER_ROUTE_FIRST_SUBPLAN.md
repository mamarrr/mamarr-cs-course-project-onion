# Phase 14 - Workspace Switcher Route-First Subplan

Parent plan: `BLL_WEBAPP_AKAVER_REFACTOR_PLAN.md`

## Scope

Make the Portal workspace switcher prefer direct route URLs for customer and resident contexts when the catalog contains enough route data.

## Guardrails

- Do not refactor API controllers.
- Do not change database schema, migrations, Base projects, Admin, or tests.
- Keep cookies as remembered context fallback only.
- Keep BLL/WebApp boundaries intact: WebApp generates URLs; BLL provides route-neutral catalog data.

## Implementation Steps

1. Add route data fields to existing catalog option DTOs: option slug and management company slug.
2. Populate customer and resident workspace catalog options from existing DAL projections.
3. Update customer/resident BLL access services so direct route URLs authorize customer representatives for their customer and residents for their own resident context, while preserving management-role access.
4. Update WebApp workspace resolver to generate customer/resident direct URLs with `PortalRouteNames`.
5. Keep `SetContext` fallback in the switcher for any option missing route data.
6. Run source-only scans and ask the project owner to build.

## Acceptance Checks

- Management, customer, and resident workspace switch options use direct route URLs when available.
- Direct customer/resident URLs have matching BLL authorization for the represented user context.
- The switcher still falls back to `SetContext` if route data is unavailable.
- No schema or migration changes are introduced.
- Build is delegated to the project owner.
