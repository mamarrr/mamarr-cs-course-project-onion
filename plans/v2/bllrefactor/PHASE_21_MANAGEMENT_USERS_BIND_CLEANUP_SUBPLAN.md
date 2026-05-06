# Phase 21 - Management Users Bind Cleanup Subplan

Parent plan: `BLL_WEBAPP_AKAVER_REFACTOR_PLAN.md`

## Scope

Remove remaining `[Bind(Prefix = ...)]` usage from Portal/Public controllers by binding the strongly typed page view models already used by the forms.

## Guardrails

- Do not change BLL, DAL, API controllers, Base projects, schema, migrations, or tests.
- Preserve current management-user add and ownership-transfer behavior.
- Keep validation messages attached to the nested form fields rendered by the current views.

## Implementation Steps

1. Change the add-user post action to accept `UsersPageViewModel` and read `AddUser`.
2. Change the ownership-transfer post action to accept `TransferOwnershipPageViewModel` and read `Transfer`.
3. Update manual validation error keys to match nested ViewModel fields.
4. Run source-only scans and ask the project owner to build.

## Acceptance Checks

- No `[Bind(...)]` remains in Portal/Public controllers.
- Add-user and transfer-ownership forms still bind to their existing nested view model properties.
- Build is delegated to the project owner.
