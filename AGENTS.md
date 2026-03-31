# Multi-Tenant Property Maintenance CRM Agent Guide

## 1. Purpose

This repository implements a multi-tenant property maintenance CRM with ticket lifecycle management, role-based access control, and strict tenant data isolation.

Primary goals:
- Keep tenant data isolated by management company boundaries.
- Enforce role-based permissions and prevent IDOR.
- Support the full ticket lifecycle from creation to closure.
- Maintain localization in UI and database for English and Estonian.
- Keep API contracts stable, versioned, and DTO-first.

## 2. Project context and repository map

Core repository structure:
- `App.Domain`: domain entities and identity entities.
- `App.DAL.EF`: EF Core context, mapping, migrations, seeding.
- `App.DTO`: public API DTO contracts by version.
- `WebApp`: MVC UI, REST API controllers, startup configuration.
- `Docs/schema.sql`: domain schema reference.

Related localization and base utilities:
- `Base.Domain/LangStr.cs`
- `App.Resources`

## 3. Architecture boundaries and layering rules

### 3.1 Domain layer

Domain entities belong in `App.Domain`. Domain types must not depend on MVC, API, or persistence-specific behavior.

### 3.2 DAL layer

Persistence concerns belong in `App.DAL.EF`, including:
- EF Core configuration.
- Value conversions such as `LangStr` to `jsonb`.
- Migrations.
- Seeding and idempotent initialization logic.

### 3.3 BLL or application service layer

Business rules, workflow, tenant checks, lifecycle transitions, permission checks, and authorization business rules must be centralized in dedicated BLL services within a dedicated BLL layer/project. Controllers should orchestrate inputs and outputs, not embed business policy.

Service-layer and business logic placement is always in the dedicated BLL layer/project, never inline in controllers and never as interim `WebApp` services.

### 3.4 Web layer

`WebApp` hosts both MVC and REST API:
- MVC controllers return views and use view models.
- API controllers return versioned DTOs.
- Do not return domain entities from public API endpoints.

## 4. Multi-tenancy and IDOR prevention policy

Tenant boundary is management company.

### 4.1 Tenant key and scope

Most operational entities are tenant-scoped via management company foreign keys in `Docs/schema.sql`. Lookup tables are global shared static data.

### 4.2 Mandatory read rules

For every tenant-scoped query:
- Filter by current tenant membership.
- Filter by user role permissions.
- Never fetch by ID only without tenant and authorization constraints.

### 4.3 Mandatory write rules

For create, update, delete actions:
- Validate actor has permission within current tenant.
- Validate all referenced foreign keys belong to the same tenant unless explicitly global lookup.
- Block cross-tenant references and return authorization or validation error.

### 4.4 IDOR checklist for each endpoint

For each MVC action and API action:
- Resolve current actor identity.
- Resolve actor tenant context.
- Apply tenant filter in query before materialization.
- Apply role-based action permission.
- Return not found or forbidden without leaking cross-tenant existence details.

## 5. RBAC model and permission matrix

### 5.1 Personas

System personas:
- `SystemAdmin`
- Management company roles: `Manager`, `Support specialist`, `Finance`
- Customer representative roles: `Primary`, `Technical`, `Billing`
- `Resident`

### 5.2 Permission matrix baseline

- Resident: create own ticket, view own tickets, add requested details, confirm completion for own tickets.
- Management roles: assign vendor, schedule work, progress status, review work logs, close ticket.
- Customer representatives: view customer portfolio tickets, comment, monitor, confirm resolution.
- SystemAdmin: platform-level administration and global maintenance.

### 5.3 Implementation note

Current startup identity role seeding uses `user` and `admin` in `App.DAL.EF/Seeding/InitialData.cs`. Business roles above remain required domain target behavior and should be aligned with identity and authorization policies.

## 6. Ticket lifecycle rules

Canonical lifecycle:

Created -> Assigned -> Scheduled -> In Progress -> Completed -> Closed

Transition guards:
- Created -> Assigned requires responsible management action.
- Assigned -> Scheduled requires vendor and schedule details.
- Scheduled -> In Progress requires work start confirmation.
- In Progress -> Completed requires work outcome and logs.
- Completed -> Closed requires resolution verification.

Recommended rule: do not skip intermediate states unless explicitly approved by business policy and audited.

## 7. API standards and versioning

### 7.1 Versioning

Use URL versioning consistently for all REST endpoints. Existing versioned style appears in `WebApp/ApiControllers/Identity/AccountController.cs`.

### 7.2 DTO-first API contract

Public API must use DTOs from `App.DTO/v1`. Do not expose domain entities directly.

### 7.3 HTTP behavior

- Use consistent status codes.
- Use structured error responses compatible with `App.DTO/v1/RestApiErrorResponse.cs`.
- Keep model validation and business validation explicit.

### 7.4 Auth and Swagger

JWT auth and Swagger are configured in `WebApp/Program.cs` and `WebApp/ConfigureSwaggerOptions.cs`. Keep API documentation and security scheme aligned with actual endpoint requirements.

## 8. Authentication and authorization requirements

- Authentication must be configured and applied before authorization in the HTTP pipeline.
- Authorization policy must enforce role and tenant scope, not role only.
- Never rely on client-provided tenant identifiers as a trusted source.

Security hardening expectations:
- Do not enable sensitive data logging outside controlled development scenarios.
- Keep HTTPS metadata and production JWT settings secure.

## 9. Localization rules

Supported cultures are English and Estonian. Localizable domain fields must use `LangStr`.

LangStr usage source:
- `Base.Domain/LangStr.cs`

Persistence rules:
- Persist `LangStr` as PostgreSQL `jsonb` via EF conversion and `jsonb` column type.

MVC flow rules:
- Create flow accepts localized input string and maps to `LangStr` entity field.
- Edit flow updates translation in current culture while preserving other stored translations.
- Render localized values through `LangStr` string conversion behavior.

Translation fallback behavior follows `LangStr` implementation in `Base.Domain/LangStr.cs`.

## 10. Database, migrations, and startup data workflow

### 10.1 Schema reference

`Docs/schema.sql` defines the domain-level schema contract and relationships.

### 10.2 Migrations

All model changes affecting persistence must include EF migration updates in `App.DAL.EF/Migrations`.

### 10.3 Startup data initialization

Data initialization switches are configured in `WebApp/appsettings.json`. Initialization logic is in `App.DAL.EF/Seeding/AppDataInit.cs`.

Use data initialization flags carefully outside local development.

## 11. Lookup and reference seeding policy

Lookup tables from `Docs/schema.sql` are global shared static data and are not tenant-specific.

General seeding rules:
- `CODE` values are immutable after release.
- Labels are localizable `LangStr` JSON with `en` and `et-ee` keys.
- Seeding is idempotent by `CODE`.
- Where `CODE` unique DB constraint is missing, enforce uniqueness in seed logic and add migration constraint when feasible.

Required initial lookup records:

- `MCOMPANY_ROLE`
  - `MANAGER` => `{ "en": "Manager", "et-ee": "Haldur" }`
  - `SUPPORT` => `{ "en": "Support specialist", "et-ee": "Tugispetsialist" }`
  - `FINANCE` => `{ "en": "Finance", "et-ee": "Finants" }`

- `CUSTOMER_REPRESENTATIVE_ROLE`
  - `PRIMARY` => `{ "en": "Primary representative", "et-ee": "Peamine esindaja" }`
  - `TECHNICAL` => `{ "en": "Technical representative", "et-ee": "Tehniline esindaja" }`
  - `BILLING` => `{ "en": "Billing representative", "et-ee": "Arvelduse esindaja" }`

- `CONTACT_TYPE`
  - `EMAIL` => `{ "en": "Email", "et-ee": "E-post" }`
  - `PHONE` => `{ "en": "Phone", "et-ee": "Telefon" }`
  - `ADDRESS` => `{ "en": "Address", "et-ee": "Aadress" }`

- `PROPERTY_TYPE`
  - `APARTMENT_BUILDING` => `{ "en": "Apartment building", "et-ee": "Korterelamu" }`
  - `PRIVATE_HOUSE` => `{ "en": "Private house", "et-ee": "Eramu" }`
  - `COMMERCIAL` => `{ "en": "Commercial property", "et-ee": "Ärikinnisvara" }`

- `LEASE_ROLE`
  - `TENANT` => `{ "en": "Tenant", "et-ee": "Üürnik" }`
  - `OWNER` => `{ "en": "Owner", "et-ee": "Omanik" }`
  - `CO_TENANT` => `{ "en": "Co-tenant", "et-ee": "Kaasüürnik" }`
  - Note: enforce `CODE` uniqueness in seeding logic.

- `TICKET_CATEGORY`
  - `PLUMBING` => `{ "en": "Plumbing", "et-ee": "Torutööd" }`
  - `ELECTRICAL` => `{ "en": "Electrical", "et-ee": "Elektritööd" }`
  - `HVAC` => `{ "en": "Heating and ventilation", "et-ee": "Küte ja ventilatsioon" }`
  - `GENERAL` => `{ "en": "General maintenance", "et-ee": "Üldhooldus" }`

- `TICKET_STATUS`
  - `CREATED` => `{ "en": "Created", "et-ee": "Loodud" }`
  - `ASSIGNED` => `{ "en": "Assigned", "et-ee": "Määratud" }`
  - `SCHEDULED` => `{ "en": "Scheduled", "et-ee": "Planeeritud" }`
  - `IN_PROGRESS` => `{ "en": "In progress", "et-ee": "Töös" }`
  - `COMPLETED` => `{ "en": "Completed", "et-ee": "Lõpetatud" }`
  - `CLOSED` => `{ "en": "Closed", "et-ee": "Suletud" }`
  - Note: enforce `CODE` uniqueness in seeding logic.

- `TICKET_PRIORITY`
  - `LOW` => `{ "en": "Low", "et-ee": "Madal" }`
  - `MEDIUM` => `{ "en": "Medium", "et-ee": "Keskmine" }`
  - `HIGH` => `{ "en": "High", "et-ee": "Kõrge" }`
  - `URGENT` => `{ "en": "Urgent", "et-ee": "Kiire" }`
  - Note: enforce `CODE` uniqueness in seeding logic.

- `WORK_STATUS`
  - `SCHEDULED` => `{ "en": "Scheduled", "et-ee": "Planeeritud" }`
  - `IN_PROGRESS` => `{ "en": "In progress", "et-ee": "Töös" }`
  - `DONE` => `{ "en": "Done", "et-ee": "Valmis" }`
  - `CANCELLED` => `{ "en": "Cancelled", "et-ee": "Tühistatud" }`

## 12. UI guidance

### 12.1 Admin

Admin UI lives in `WebApp/Areas/Admin` and is restricted to `SystemAdmin` role. Admin CRUD scaffolding is performed manually by the project owner. AI contributors should delegate explicit admin scaffolding tasks back to the user.

### 12.2 Client UX style

The client UI should be energetic, responsive, and professional, with card-based information layout and clear action feedback.

- Show immediate success confirmation when actions complete.
- Show field-level and summary validation errors with actionable guidance.
- Prevent duplicate submissions for long-running actions.
- Keep mobile layouts compact and touch-friendly.

### 12.3 Overall UI concept

- Header:
  - Contextual search bar.
  - Right-side profile shortcut.
- Left navigation:
  - Company name.
  - Main navigation links.
  - New ticket action.
  - Support.
  - Logout.
- Main area:
  - Dashboard-first card layout.

### 12.4 Management company dashboard

Intent:
- Operational control center for company-level lifecycle management and vendor coordination.

Key pages:
- Dashboard, Tickets, Properties, Customers, Vendors.

Primary actions:
- Create ticket for customer/resident.
- Assign/reassign vendor.
- Schedule work.
- Progress ticket status.
- Review work logs and close ticket.

Key widgets:
- Occupancy overview.
- Active maintenance summary.
- Priority and status distributions.
- Top unresolved properties (top 3).
- Top active vendors (top 3).
- Recently added tickets.
- Upcoming scheduled work.

### 12.5 Customer dashboard

Intent:
- Portfolio transparency for maintenance progress and unresolved blockers.

Key pages:
- Dashboard, Tickets, Properties, Updates, Profile.

Primary actions:
- Track lifecycle and status history.
- Add comments.
- Confirm or challenge resolution.
- Escalate unresolved concerns.

Key widgets:
- Open tickets by priority and status.
- Delayed or awaiting-vendor items.
- Recently completed tickets awaiting confirmation.
- Property hotspots.
- Recent activity timeline.

### 12.6 Resident dashboard

Intent:
- Simple personal workspace for submitting and tracking own maintenance issues.

Key pages:
- Dashboard, My Tickets, New Ticket, Support, Profile.

Primary actions:
- Create ticket with category, priority, description, attachments.
- Track lifecycle progression.
- Provide requested follow-up details.
- Confirm completion.

Key widgets:
- Open tickets with latest update.
- Upcoming scheduled visits.
- Recently closed tickets.
- Quick new-ticket action.
- Notifications requiring resident response.

## 13. Testing expectations

Contributors must validate changes with tests proportional to scope.

Minimum expected coverage areas:
- Tenant isolation and IDOR protection for read and write paths.
- RBAC authorization outcomes for critical actions.
- Ticket lifecycle transition rules and invalid transition rejection.
- Localization fallback behavior and LangStr persistence format.
- Seeding idempotency and `CODE` uniqueness behavior.

## 14. Coding standards and quality gates

- Respect nullable strictness and warning policy from `Directory.Build.Props`.
- Prefer async request and data-access paths.
- Keep controllers thin; centralize business rules in services.
- Keep API contracts versioned and stable.
- Keep migrations and seed updates synchronized with schema intent.

## 15. Definition of done

A change is complete only when all apply:
- Tenant and IDOR checks are implemented and verified.
- RBAC enforcement is present for affected actions.
- API changes use versioned DTO contracts and documented behavior.
- Localization and `LangStr` rules are applied where needed.
- Migrations and seed updates are included when persistence changes.
- Tests cover critical behavior and regressions.
- UI changes provide clear success/failure feedback.

## 16. AI contributor guardrails

AI contributors must:
- Preserve tenant isolation and ownership checks.
- Avoid exposing domain entities directly via public API.
- Keep lookup `CODE` values stable and never repurpose them.
- Keep implementation aligned with `Docs/schema.sql`, `WebApp/Program.cs`, and DAL mappings.
- Delegate explicit Admin scaffolding requests back to the user.
- Avoid destructive schema/seed changes without explicit requirement and migration plan.

## 17. Open questions to finalize

1. Finalized decision: default culture source of truth is English (`en`).
2. Open question: identity role plan should be finalized; startup seeding currently includes `user` and `admin`, while domain flows require broader operational role mapping.
3. Finalized decision: service-layer and business logic placement is always in a dedicated BLL layer/project, with no interim `WebApp` service placement.
