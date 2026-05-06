# Phase 16 - Customer MVC Mapper Boundary Subplan

Parent plan: `BLL_WEBAPP_AKAVER_REFACTOR_PLAN.md`

## Scope

Remove Portal controller dependency on API mapper code for customer workspace query mapping.

## Guardrails

- Do not refactor API controllers or API DTOs.
- Do not change BLL, DAL, Base projects, tests, schema, or migrations.
- Keep MVC and API mapping independent.
- Keep behavior identical: Portal customer controllers should still construct the same BLL query values.

## Implementation Steps

1. Add a customer MVC mapper for `GetCustomerWorkspaceQuery`.
2. Replace Portal customer controller injections/usages of `CustomerWorkspaceApiMapper` with the MVC mapper.
3. Leave `CustomerWorkspaceApiMapper` registered for API controllers.
4. Run source-only scans and ask the project owner to build.

## Acceptance Checks

- No Portal controller references `WebApp.Mappers.Api.*`.
- API controllers still use the API mapper.
- Portal controllers still call BLL through `_bll`.
- Build is delegated to the project owner.
