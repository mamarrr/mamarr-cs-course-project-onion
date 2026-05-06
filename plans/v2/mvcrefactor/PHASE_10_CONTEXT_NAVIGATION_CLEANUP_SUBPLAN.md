# Phase 10: Context Navigation Cleanup

Goal: make Portal context switching prefer concrete route URLs where they are
already available, while keeping cookie-based selection as a fallback for
contexts that do not yet have route-ready identifiers.

Scope:
- Use `WorkspaceSwitchOptionViewModel.Url` in the workspace switcher when set.
- Keep `SetContext` links for customer and resident contexts until the BLL
  workspace catalog exposes enough slug data for route-first destinations.
- Guard invalid context selection types in WebApp before constructing BLL
  queries, and reuse the shared context-cookie options helper.
- Keep `ICurrentPortalContextResolver` as the single user-id route-context reader
  for WebApp UI services.

Explicit deferrals:
- Customer switcher URLs need customer slug plus company slug in the workspace
  catalog.
- Resident root URLs need a route-first resident context selection design.
- MVC/API mapper claim parsing cleanup is a separate mechanical phase because it
  touches many controller calls.

Out of scope:
- No BLL, DAL, Base, API, Admin, schema, migration, or test changes.
