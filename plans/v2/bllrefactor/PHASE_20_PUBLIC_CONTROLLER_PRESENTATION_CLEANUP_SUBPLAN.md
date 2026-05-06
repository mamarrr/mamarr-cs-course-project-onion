# Phase 20 - Public Controller Presentation Cleanup Subplan

Parent plan: `BLL_WEBAPP_AKAVER_REFACTOR_PLAN.md`

## Scope

Remove redundant Public controller presentation state where the corresponding views already set layout titles.

## Guardrails

- Do not redesign Public layouts.
- Do not remove `ViewData["Title"]` from Public views in this phase because `_Layout` and `_OnboardingLayout` still use it for document titles.
- Do not change onboarding behavior, redirects, cookies, API controllers, BLL, DAL, Base projects, schema, migrations, or tests.

## Implementation Steps

1. Remove redundant `ViewData["Title"]` assignments from Public onboarding controller actions whose views already set title from the ViewModel.
2. Remove unused controller helper code left behind by earlier presentation logic.
3. Run source-only scans and ask the project owner to build.

## Acceptance Checks

- `OnboardingController` no longer sets `ViewData["Title"]`.
- Public views still set document titles for the current layout.
- Behavior and redirects are unchanged.
- Build is delegated to the project owner.
