# Multi-Tenant Property Maintenance CRM Agent Guide

## 1. Purpose

This repository implements a multi-tenant property maintenance CRM for ticket lifecycle management, role-based access control, and strict tenant data isolation.

Primary goals:
- Keep tenant data isolated by management company boundaries.
- Enforce role-based permissions and prevent IDOR.
- Support the ticket lifecycle from creation to closure.
- Maintain English and Estonian localization in UI and persisted domain data.
- Keep public API contracts stable, versioned, and DTO-first.

## 2. Repository Map

Core projects and references:
- `App.Domain`: domain entities and identity entities.
- `App.DAL.EF`: EF Core context, mappings, migrations, seeding, and value conversions.
- `App.DTO`: public API DTO contracts by version.
- `WebApp`: MVC UI, REST API controllers, startup configuration, auth, Swagger, and localization middleware.
- `Base.Domain/LangStr.cs`: persisted multilingual value object.
- `App.Resources`: `.resx` resources for static UI and validation text.
- `Docs/schema.sql`: domain schema reference.

## 3. Architecture Rules

Domain entities belong in `App.Domain` and must not depend on MVC, API, or persistence-specific behavior.

Persistence concerns belong in `App.DAL.EF`, including EF configuration, `LangStr` to `jsonb` conversion, migrations, and idempotent initialization logic.

Business rules, workflow, tenant checks, lifecycle transitions, permission checks, and authorization business rules must live in dedicated BLL services within a dedicated BLL layer/project. Do not place business policy inline in controllers or create interim `WebApp` services for it. Keep BLL services small and focused on one bounded responsibility.

`WebApp` hosts MVC and REST API surfaces:
- MVC controllers return views and use strongly typed view models.
- API controllers return versioned DTOs from `App.DTO`.
- Public API endpoints must never return domain entities.
- MVC and API controllers map transport-specific request models to shared BLL contracts independently.
- Split MVC controllers by feature section or subpage when navigation separates those areas.

## 4. Security, Tenancy, and Authorization

Tenant boundary is management company. Most operational entities are tenant-scoped through management company relationships in `Docs/schema.sql`; lookup tables are global shared static data.

For every tenant-scoped read:
- Resolve current actor identity and tenant context.
- Apply tenant membership and role permission filters before materialization.
- Never fetch by ID only without tenant and authorization constraints.
- Return not found or forbidden without leaking cross-tenant existence details.

For every tenant-scoped create, update, or delete:
- Validate actor permissions inside the current tenant.
- Validate referenced foreign keys belong to the same tenant unless they are explicitly global lookups.
- Block cross-tenant references with authorization or validation errors.

Authentication must run before authorization in the HTTP pipeline. Authorization policy must enforce tenant scope and role, not role alone. Never trust client-provided tenant identifiers as authority.

Security hardening:
- Do not enable sensitive data logging outside controlled development scenarios.
- Keep HTTPS metadata and production JWT settings secure.
- Preserve IDOR protections when refactoring queries or moving logic between layers.

## 5. Domain Workflows

System personas:
- `SystemAdmin`
- Management company roles: `Manager`, `Support specialist`, `Finance`
- Customer representative roles: `Primary`, `Technical`, `Billing`
- `Resident`

Current startup identity seeding uses `user` and `admin` in `App.DAL.EF/Seeding/InitialData.cs`. The operational roles above remain target behavior and must be aligned with identity roles and authorization policies as the domain matures.

Permission baseline:
- Residents create and view own tickets, add requested details, and confirm completion for own tickets.
- Management roles assign vendors, schedule work, progress status, review work logs, and close tickets.
- Customer representatives view customer portfolio tickets, comment, monitor, and confirm or challenge resolution.
- `SystemAdmin` handles platform-level administration and global maintenance.

Canonical ticket lifecycle:

`Created -> Assigned -> Scheduled -> In Progress -> Completed -> Closed`

Transition guards:
- `Created -> Assigned` requires responsible management action.
- `Assigned -> Scheduled` requires vendor and schedule details.
- `Scheduled -> In Progress` requires work start confirmation.
- `In Progress -> Completed` requires work outcome and logs.
- `Completed -> Closed` requires resolution verification.

Do not skip intermediate states unless explicit business policy permits it and the action is audited.

## 6. API Standards

Use URL versioning consistently for REST endpoints. Existing style is in `WebApp/ApiControllers/Identity/AccountController.cs`.

Public API rules:
- Use DTOs from `App.DTO/v1` or the appropriate versioned DTO namespace.
- Do not expose domain entities.
- Use consistent HTTP status codes.
- Use structured error responses compatible with `App.DTO/v1/RestApiErrorResponse.cs`.
- Keep model validation and business validation explicit.
- Keep Swagger documentation and JWT security definitions aligned with endpoint requirements.

JWT auth and Swagger configuration live in `WebApp/Program.cs` and `WebApp/ConfigureSwaggerOptions.cs`.

## 7. Localization

Supported cultures are English and Estonian. Use the correct localization mechanism:
- `LangStr` is for persisted multilingual domain data that can differ per record, such as lookup labels, job titles, and user-entered localized entity fields.
- `.resx` resources in `App.Resources` are for static UI text, labels, headings, validation messages, select placeholders, buttons, and user-visible controller or service messages.

Do not use `.resx` resources as a substitute for persisted multilingual business data. Do not use `LangStr` for static UI chrome.

`LangStr` rules:
- Domain fields that are localizable and persisted must use `LangStr`, not parallel language columns or serialized JSON in plain strings.
- Persist `LangStr` as PostgreSQL `jsonb` through EF conversion.
- Current `LangStr` behavior stores neutral culture keys: `en` and `et`, not `en-US` or `et-EE`.
- Default source-of-truth language is English (`en`).
- Create flows may map the current-culture form string into a `LangStr` property.
- Edit flows must update the existing value with `SetTranslation(...)` so other translations are preserved.
- Views and UI projections render localized domain values with `ToString()` or the equivalent `LangStr` display conversion.
- Do not render raw JSON, dictionaries, or serialized `LangStr` payloads.
- Do not reimplement fallback logic in controllers or Razor; rely on `LangStr`.

Resource rules:
- ViewModels must use resource-backed `[Display]`, `[Required]`, `[StringLength]`, and other user-visible validation attributes.
- User-visible controller and service messages must come from resources, or from codes that the web layer maps to resources.
- Add or update English and Estonian resource entries together.
- Static select placeholders such as "Select role" belong in resources; persisted role labels loaded from DB belong in `LangStr`.

Localization references:
- `Base.Domain/LangStr.cs`
- `WebApp/Program.cs`
- `WebApp/Areas/Admin/Controllers/ContactTypeController.cs`
- `WebApp/ViewModels/Onboarding/LoginViewModel.cs`
- `WebApp/ViewModels/Onboarding/RegisterViewModel.cs`
- `WebApp/ViewModels/Onboarding/CreateManagementCompanyViewModel.cs`
- `App.BLL/ManagementUsers/ManagementUserAdminService.cs`
- `App.Resources`

## 8. Database, Migrations, and Seeding

`Docs/schema.sql` is the domain schema reference. Keep EF mappings, migrations, and seed data aligned with it.

All model changes affecting persistence must include EF migrations in `App.DAL.EF/Migrations`. Keep migration column types synchronized with model intent, especially `jsonb` columns for `LangStr`.

Startup data initialization is configured in `WebApp/appsettings.json` and implemented in `App.DAL.EF/Seeding/AppDataInit.cs`. Use initialization switches carefully outside local development.

Lookup and reference data rules:
- Lookup tables are global shared static data, not tenant-specific.
- `CODE` values are stable invariant identifiers and must not be repurposed.
- User-facing lookup labels are localized `LangStr` values stored with `en` and `et` keys.
- Seeding must be idempotent by `CODE`.
- Enforce `CODE` uniqueness in seed logic; add DB uniqueness constraints when feasible.
- Keep seeded examples and real payloads aligned with current neutral-key `LangStr` behavior.

## 9. UI and MVC Standards

Admin UI lives in `WebApp/Areas/Admin` and is restricted to the `SystemAdmin` role. Admin CRUD scaffolding is performed manually by the project owner; AI contributors should delegate explicit admin scaffolding tasks back to the user.

Client UI should be responsive, professional, and clear:
- Show immediate success confirmation when actions complete.
- Show field-level and summary validation errors with actionable guidance.
- Prevent duplicate submissions for long-running actions.
- Keep mobile layouts compact and touch-friendly.

Primary application shell:
- Header with contextual search and profile shortcut.
- Left navigation with company name, main links, new-ticket action, support, and logout.
- Dashboard-first main area.

Dashboard intent:
- Management company: operational control for lifecycle management and vendor coordination.
- Customer: portfolio transparency for maintenance progress and unresolved blockers.
- Resident: simple workspace for submitting and tracking own maintenance issues.

MVC rules:
- Avoid `ViewBag` and `ViewData`.
- `TempData` is allowed for redirect-scoped confirmation messages, error info, and other small PRG flash state.
- Use strongly typed ViewModels for MVC views and actions unless a framework constraint makes that impossible.
- Compose shared layout or page-shell data from reusable types such as `PageShell` instead of duplicating navigation/context fields.
- Do not use `[Bind]` when a dedicated strongly typed ViewModel with explicit mapping is practical.

## 10. Testing and Quality Gates

OVERRIDE TESTING: DO NOT WRITE TESTS UNTIL FURTHER NOTICE

When the testing override is lifted, validate changes proportionally to scope, especially:
- Tenant isolation and IDOR protection for read and write paths.
- RBAC outcomes for critical actions.
- Ticket lifecycle transition rules and invalid transition rejection.
- Localization fallback behavior and `LangStr` persistence format.
- Seeding idempotency and `CODE` uniqueness behavior.

Quality rules:
- Respect nullable strictness and warning policy from `Directory.Build.Props`.
- Prefer async request and data-access paths.
- Keep controllers thin and business policy centralized in BLL services.
- Keep API contracts versioned and stable.
- Keep migrations and seed updates synchronized with schema intent.

## 11. Definition of Done

A change is complete only when applicable items are handled:
- Tenant and IDOR checks are implemented and verified.
- RBAC enforcement is present for affected actions.
- API changes use versioned DTO contracts and documented behavior.
- Localization and `LangStr` rules are applied where needed.
- Migrations and seed updates are included when persistence changes.
- UI changes provide clear success/failure feedback.
- Required verification has been run, except where the current testing override blocks writing tests.

## 12. Contributor Guardrails

AI contributors must:
- Preserve tenant isolation and ownership checks.
- Avoid exposing domain entities directly through public API endpoints.
- Keep lookup `CODE` values stable.
- Keep implementation aligned with `Docs/schema.sql`, `WebApp/Program.cs`, and DAL mappings.
- Keep BLL placement strict: business logic belongs in the dedicated BLL layer/project.
- Delegate explicit Admin scaffolding requests back to the user.
- Avoid destructive schema or seed changes without an explicit requirement and migration plan.
- Keep `AGENTS.md` concise and repo-specific; move long explanations, historical bug narratives, and detailed implementation walkthroughs to docs or plans.
