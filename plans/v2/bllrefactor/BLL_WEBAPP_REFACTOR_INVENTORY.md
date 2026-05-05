# BLL + WebApp Refactor Inventory

Date: 2026-05-05

This inventory supports `BLL_WEBAPP_AKAVER_REFACTOR_PLAN.md`.

## BLL

- `IAppBLL` already exists and extends `IBaseBLL`.
- `AppBLL` already extends `BaseBLL<IAppUOW>` and composes services from `IAppUOW`.
- BLL projects do not reference `App.DAL.EF`, `WebApp`, MVC, Razor, `App.DTO`, or domain entities directly.
- `App.BLL.Contracts` does not expose DAL DTOs in public service method signatures.
- `App.BLL` implementation uses DAL DTOs internally, which is expected at the BLL-to-DAL boundary.
- BLL methods mostly return `FluentResults.Result` or `Result<T>`.
- Typed common errors already exist: unauthorized, forbidden, not found, validation, conflict, business rule, unexpected.
- Current BLL DTO project contains commands, queries, workflow models, projections, errors, and constants.
- Canonical CRUD BLL DTOs are missing.
- Existing BLL mappers are static projection mappers and do not implement `Base.Contracts.IBaseMapper`.

## WebApp

- Current MVC areas are `Admin`, `Management`, `Customer`, `Property`, `Unit`, and `Resident`, plus root controllers.
- Target areas remain `Public`, `Portal`, and `Admin`; this is a later route/area phase.
- Admin controllers are scaffolded and inject `AppDbContext`. This remains temporary accepted technical debt.
- No non-Admin MVC controller currently injects `AppDbContext`, `IAppUOW`, repositories, or `App.DAL.EF`.
- Root `OnboardingController` already injects `IAppBLL`.
- Protected MVC controllers mostly inject individual BLL services instead of `IAppBLL`.
- UI layout/chrome services are WebApp-local and do not use EF directly.
- Current route style is deeper than the target:
  - `/m/{companySlug}/c/{customerSlug}`
  - `/m/{companySlug}/c/{customerSlug}/p/{propertySlug}`
  - `/m/{companySlug}/c/{customerSlug}/p/{propertySlug}/u/{unitSlug}`
  - `/m/{companySlug}/r/{residentIdCode}`

## Known Exceptions

- API controllers are out of scope for this refactor slice.
- `WebApp/ApiControllers/Identity/AccountController.cs` injects `AppDbContext`; this is not handled here because API refactoring is explicitly out of scope.
- Admin direct EF usage is isolated to `WebApp/Areas/Admin` and remains scaffolded technical debt.
- Workflow DTOs stay when they express access, onboarding, lease assignment, ticket lifecycle, membership, ownership transfer, or dashboard/read projections.

