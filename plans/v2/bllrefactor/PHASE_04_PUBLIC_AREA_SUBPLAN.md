# Phase 4 Public Area Subplan

This subplan covers Public area extraction.

## Goals

- Move public/auth/onboarding MVC controllers and views under `Areas/Public`.
- Preserve legacy `/Onboarding/...` and `/Home/...` URLs during transition.
- Add target short public URLs such as `/`, `/login`, `/register`, `/onboarding`, `/logout`, `/set-language`, and `/privacy`.
- Keep protected Management/Customer/Property/Unit/Resident areas untouched in this slice.
- Avoid API, Admin, Base, DAL, test, schema, and migration changes.

## Scope

- `HomeController`
- `OnboardingController`
- Home views
- Onboarding views
- Public route and link updates
- Onboarding guard public-route exceptions

## Deferred Scope

- Portal area consolidation.
- Protected route migration.
- Context resolver redesign.
- API controller cleanup.
- Admin scaffold cleanup.

## Build Checkpoint

After this slice, the project owner should run:

```powershell
dotnet build mamarrproject.sln -nologo
```

