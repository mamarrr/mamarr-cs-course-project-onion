# Phase 08: Portal Area Reorganization

Goal: reduce protected MVC application areas to a single `Portal` area while
preserving current route templates and controller behavior.

Scope:
- Move protected Management, Customer, Property, Unit, and Resident controllers
  under `WebApp/Areas/Portal/Controllers/{Feature}`.
- Move their views under `WebApp/Areas/Portal/Views/{Feature}`.
- Change protected controllers from their old areas to `[Area("Portal")]`.
- Add a Portal feature view-location expander so duplicate controller names
  such as `DashboardController` and `ProfileController` resolve feature views.
- Update hardcoded Portal view paths, route defaults, and protected `asp-area`
  references.
- Keep customer/resident onboarding redirects on the chooser when no route-first
  slug destination is available; concrete shortcut routes are Phase 09 scope.

Out of scope:
- No route-shape refactor beyond area defaults.
- No BLL service or DTO changes.
- No API, Admin, DAL, Base, schema, migration, or test changes.
