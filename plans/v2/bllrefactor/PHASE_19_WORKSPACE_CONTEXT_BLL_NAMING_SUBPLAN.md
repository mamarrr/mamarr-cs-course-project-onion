# Phase 19 - Workspace Context BLL Naming Subplan

Parent plan: `BLL_WEBAPP_AKAVER_REFACTOR_PLAN.md`

## Scope

Rename API-shaped BLL onboarding context contracts to API-neutral workspace context names while preserving behavior and API controller responses.

## Guardrails

- Do not change API DTOs or API response shapes.
- Do not refactor API controllers beyond calling the renamed BLL facade property.
- Do not change DAL, Base projects, schema, migrations, or tests.
- Keep WebApp API mappers responsible for mapping BLL models to `App.DTO`.

## Implementation Steps

1. Rename `IApiOnboardingContextService` to `IWorkspaceContextService`.
2. Rename `ApiOnboardingContextCatalogModel` and `ApiOnboardingContextModel` to workspace-context model names.
3. Rename `ApiWorkspaceContextService` to `WorkspaceContextService`.
4. Rename `IAppBLL.ApiOnboardingContexts` to `WorkspaceContexts`.
5. Update DI, `AppBLL`, API controllers, and API route-context mapper references.
6. Run source-only scans and ask the project owner to build.

## Acceptance Checks

- BLL contracts and DTOs no longer use `ApiOnboardingContext*` names.
- API controllers still map BLL workspace-context models into API DTO responses.
- No `App.DTO` types are introduced into BLL.
- Build is delegated to the project owner.
