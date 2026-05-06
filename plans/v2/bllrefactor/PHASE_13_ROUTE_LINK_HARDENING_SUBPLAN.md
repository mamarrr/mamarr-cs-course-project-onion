# Phase 13 - Route Link Hardening Subplan

Parent plan: `BLL_WEBAPP_AKAVER_REFACTOR_PLAN.md`

## Scope

Harden Portal MVC links that cross between feature controller groups, without implementing deferred company-scoped property/unit shortcut routes.

## Guardrails

- Do not refactor API controllers.
- Do not change BLL, DAL, Base, schema, migrations, Admin, or tests.
- Do not implement `/m/{companySlug}/properties/{propertySlug}` or `/m/{companySlug}/units/{unitSlug}` in this phase.
- Keep current nested property/unit routes working.

## Implementation Steps

1. Add a small `PortalRouteNames` constants type for stable Portal detail route names.
2. Name the Portal detail actions used by cross-context links.
3. Replace fragile cross-context `asp-controller="Dashboard"` links with named-route tag helpers.
4. Replace remaining hardcoded `/m/...` view links with named-route tag helpers where a named target exists.
5. Run source-only scans for stale old-area links, hardcoded `/m` hrefs, and whitespace.
6. Ask the project owner to build.

## Deferred Follow-Up

- Route-name generation for chrome navigation and breadcrumbs should be paired with the company-scoped shortcut-route phase.
- Customer/resident workspace switcher direct URLs remain a separate route-first context phase.

## Acceptance Checks

- Property, unit, resident, customer, and ticket detail cross-links use route names rather than ambiguous same-area controller names.
- No Portal view uses hardcoded `href="/m..."`
- Build is delegated to the project owner.
