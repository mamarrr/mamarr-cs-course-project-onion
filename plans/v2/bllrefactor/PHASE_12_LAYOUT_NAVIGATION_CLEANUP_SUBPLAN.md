# Phase 12 - Layout and Navigation Cleanup Subplan

Parent plan: `BLL_WEBAPP_AKAVER_REFACTOR_PLAN.md`

## Scope

Clean up the Portal chrome/navigation surface without changing BLL behavior or routes.

## Guardrails

- Do not refactor API controllers.
- Do not change BLL, DAL, Base, schema, migrations, Admin, or tests.
- Keep the Phase 9 route deferrals for company-scoped property/unit shortcuts.
- Keep chrome/navigation code in WebApp and backed by `IAppBLL`.

## Implementation Steps

1. Remove temporary view-location logging from the customer dashboard controller.
2. Move dashboard section labels from `ViewData` into strongly typed Portal dashboard view models.
3. Update customer, property, unit, and resident dashboard views to read section labels from models.
4. Run source-only scans for layout provider DAL usage, old area names, stale mapper/user boundaries, and whitespace.
5. Ask the project owner to build.

## Deferred Follow-Up

- True company-scoped property and unit shortcut routes remain deferred from Phase 9 because they require controller/BLL route support, not only layout cleanup.
- Route-name standardization for chrome, breadcrumbs, and repeated `DashboardController` links should be handled with the shortcut route follow-up.
- Customer and resident workspace switcher direct URLs need richer catalog route data and should stay cookie-assisted until that BLL shape is available.

## Acceptance Checks

- Portal dashboard placeholder views no longer need `ViewData["CurrentSectionLabel"]`.
- Customer dashboard controller no longer injects `IWebHostEnvironment` only for view candidate logging.
- WebApp UI/chrome/layout providers do not use `AppDbContext`, `App.DAL.EF`, or `IAppUOW`.
- Build is delegated to the project owner.
