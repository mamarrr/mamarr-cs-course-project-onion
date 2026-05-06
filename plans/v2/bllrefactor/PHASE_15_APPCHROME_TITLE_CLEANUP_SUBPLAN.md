# Phase 15 - AppChrome Title Cleanup Subplan

Parent plan: `BLL_WEBAPP_AKAVER_REFACTOR_PLAN.md`

## Scope

Remove redundant Portal `ViewData` page-title and section-label assignments now that Portal pages use `_AppChromeLayout` and strongly typed `AppChrome.PageTitle`.

## Guardrails

- Do not change API controllers, tests, Base projects, DAL, schema, or migrations.
- Do not change Admin scaffolding.
- Do not change Public onboarding layout behavior.
- Keep Portal page titles sourced from `AppChrome.PageTitle`.

## Implementation Steps

1. Remove Portal controller `ViewData["Title"]` assignments where the same title is already passed to `AppChromeRequest.PageTitle`.
2. Remove Portal view `ViewData["Title"]` assignments under the AppChrome layout.
3. Remove the stale `ViewData["CurrentSectionLabel"]` assignment from the unit tenants view.
4. Run source-only scans and ask the project owner to build.

## Acceptance Checks

- Portal pages continue to use strongly typed AppChrome title data.
- No `ViewData["Title"]` or `ViewData["CurrentSectionLabel"]` remains in Portal controllers/views.
- Public layout title behavior is unchanged.
- Build is delegated to the project owner.
