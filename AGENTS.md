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
- MVC and future API controllers must map transport-specific request models to shared BLL contracts independently.

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

Supported cultures are English and Estonian. Localization in this repository uses two complementary mechanisms:
- `LangStr` for localizable domain data that is persisted in the database.
- `.resx` resource files in `App.Resources` for static UI text, form labels, validation messages, headings, button captions, and controller or service messages shown to the user.

Use the correct mechanism for the correct kind of text:
- Use `LangStr` when the value belongs to domain data and can differ per record, such as lookup labels, job titles, or user-entered localized entity fields.
- Use resource files when the value is application UI text that should be translated once and reused consistently, such as `Email`, `Password`, `Select role`, `Unable to update user`, or validation error strings.
- Do not use resource files as a substitute for persisted multilingual business data.
- Do not use `LangStr` as a substitute for static UI chrome.

Primary implementation references:
- `Base.Domain/LangStr.cs`
- `WebApp/Program.cs`
- `WebApp/Areas/Admin/Controllers/ContactTypeController.cs`
- `WebApp/ViewModels/Onboarding/LoginViewModel.cs`
- `WebApp/ViewModels/Onboarding/RegisterViewModel.cs`
- `WebApp/ViewModels/Onboarding/CreateManagementCompanyViewModel.cs`
- `WebApp/Controllers/OnboardingController.cs`
- `WebApp/Areas/Management/Controllers/UsersController.cs`
- `App.Resources`
- `plans/localization/localization-bugs.md`

### 9.1 Request culture, supported cultures, and pipeline expectations

Current request localization is configured in `WebApp/Program.cs`.

Contributor rules:
- Keep supported cultures aligned with application configuration and request localization middleware.
- Authentication and navigation flows must preserve culture switching behavior.
- If a UI bug mentions that language can be selected but the page does not switch language, inspect request localization configuration and language-selection flow before changing view text.
- Do not hardcode translated text in views or controllers when that text should come from resources.

### 9.2 LangStr rules for persisted multilingual domain data

`LangStr` usage source:
- `Base.Domain/LangStr.cs`

`LangStr` is the required type for localizable domain fields that must round-trip through persistence and render according to current UI culture.

#### 9.2.1 Why LangStr is required

Use `LangStr` for domain fields that must be localizable in UI and persisted as multilingual values in DB.

Do not store separate language-specific columns such as `LabelEn` and `LabelEtEe` when `LangStr` is available.

Do not serialize a `LangStr` into a plain `string` column and then render that serialized payload directly.

#### 9.2.2 Domain model pattern

In domain entities, localizable field types must be `LangStr`, not plain `string`.

Expected shape:

```csharp
public class ContactType
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public LangStr Label { get; set; } = new();
}
```

Notes:
- Keep `Code` as invariant business identifier such as `EMAIL`, `PHONE`, or `MANAGER`.
- Keep user-facing labels in `LangStr`.
- Treat `Code` as stable and non-localized.

#### 9.2.3 Actual key behavior in LangStr

`LangStr` currently normalizes culture keys to neutral language keys.

Important implementation detail from `Base.Domain/LangStr.cs`:
- The constructor stores values under the neutral culture key by splitting the current culture name on `-`.
- `SetTranslation` also stores values under the neutral culture key.
- This means current code stores `en` and `et`, not `en-US` or `et-EE`.

Implications:
- Contributor guidance must match implementation reality.
- When documenting persisted JSON examples, prefer `en` and `et` keys.
- If UI culture is `et-EE`, the stored translation key still becomes `et` in current implementation.
- Do not write new logic that assumes `et-ee` keys are persisted unless `LangStr` implementation itself is intentionally changed.

#### 9.2.4 Persistence rules

Persist `LangStr` as PostgreSQL `jsonb` via EF conversion and `jsonb` column type.

Expected stored JSON shape example under current implementation:

```json
{ "en": "Email", "et": "E-post" }
```

Persistence safeguards:
- Use a converter for `LangStr` to `jsonb` and back.
- Ensure migration column type is `jsonb`.
- Verify save/load roundtrip in tests.
- Keep migrations synchronized with model changes.

#### 9.2.5 ViewModel pattern for localized input

For MVC input forms, keep localized input fields as plain `string` in view models unless the UI explicitly edits multiple cultures at once.

Why:
- Forms normally submit one visible value for the current UI culture.
- Controllers map that one current-culture value into the entity `LangStr`.

Reference pattern:
- `WebApp/ViewModels/ContactType/ContactTypeCreateViewModel.cs`
- `WebApp/ViewModels/ContactType/ContactTypeEditViewModel.cs`

#### 9.2.6 Create flow pattern

In create flows, assign the localized form string directly to the `LangStr` entity field.

Reference in `ContactTypeController` create action:
- `Label = vm.Label`

This relies on `LangStr` conversion behavior to place the submitted value into the current culture translation bucket and ensure a default-culture value exists.

Create flow checklist:
- Validate `ModelState`.
- Map invariant fields such as `Code` normally.
- Map localized form string into the entity `LangStr` field.
- Save with EF.

#### 9.2.7 Edit flow pattern

In edit flows, update the current culture translation on the existing entity without replacing the entire `LangStr` object.

Reference in `ContactTypeController` edit action:
- `entity.Label.SetTranslation(vm.Label);`

Why this is important:
- Replacing the entire `LangStr` can drop previously saved translations.
- `SetTranslation(...)` updates only the current culture key while preserving other translations already stored in the entity.

Edit flow checklist:
- Load existing entity from DB.
- Update non-localized fields as needed.
- Call `SetTranslation` on the existing `LangStr` field.
- Save changes.

This rule directly addresses bugs like the one recorded in `plans/localization/localization-bugs.md` where changing a `JobTitle` in Estonian also changed English.

#### 9.2.8 Rendering pattern

In views and UI mapping code, render `LangStr` using `ToString()` or equivalent display conversion that relies on `LangStr.Translate()`.

References:
- `ContactType/Index.cshtml`: `@item.Label.ToString()`
- `ContactType/Details.cshtml`: `@Model.Label.ToString()`
- role and lookup projection patterns in controllers and services that use `Label.ToString()` for display text

Contributor rules:
- Do not render raw serialized JSON.
- Do not manually pick translations in Razor when `LangStr` already handles fallback.
- Do not pass raw dictionaries into display-only view models when a localized string is what the view needs.

#### 9.2.9 Culture and fallback behavior

Translation fallback behavior follows `LangStr` implementation in `Base.Domain/LangStr.cs`.

Project expectations:
- Default source-of-truth language is English with key `en`.
- Estonian translations are currently stored under neutral key `et`.
- If a full culture key such as `et-EE` is requested, `LangStr.Translate()` falls back to its neutral key.
- If the requested culture translation is missing, `LangStr` falls back to the default culture.

Contributor rules:
- Do not reimplement fallback logic in controllers or views.
- Always rely on `LangStr` behavior centrally.
- If fallback behavior must change, change `LangStr` intentionally and update tests and documentation together.

#### 9.2.10 Reference implementation with ContactType

This section explains the expected end-to-end pattern for using `LangStr` in MVC CRUD flows.

Reference implementation files:
- `WebApp/Areas/Admin/Controllers/ContactTypeController.cs`
- `WebApp/Areas/Admin/Views/ContactType/Create.cshtml`
- `WebApp/Areas/Admin/Views/ContactType/Edit.cshtml`
- `WebApp/Areas/Admin/Views/ContactType/Details.cshtml`
- `WebApp/Areas/Admin/Views/ContactType/Index.cshtml`
- `WebApp/ViewModels/ContactType/ContactTypeCreateViewModel.cs`
- `WebApp/ViewModels/ContactType/ContactTypeEditViewModel.cs`
- `WebApp/ViewModels/ContactType/ContactTypeDetailsViewModel.cs`

Reusable recipe for any localized entity:
1. Domain field type is `LangStr`.
2. EF mapping stores it as `jsonb`.
3. Create view model uses plain `string` for current-culture input.
4. Create POST maps the string to `LangStr`.
5. Edit view model uses plain `string` for current-culture input.
6. Edit POST calls `SetTranslation(...)` on the existing entity field.
7. Views render localized value with `ToString()`.
8. Tests verify translation preservation and fallback.

#### 9.2.11 Common LangStr mistakes to avoid

- Replacing an existing `LangStr` during edit and losing translations.
- Rendering raw JSON instead of relying on `LangStr.ToString()`.
- Treating a localized field as plain `string` and storing serialized multilingual payload text in it.
- Treating invariant `Code` fields as localizable display text.
- Reimplementing culture fallback in controllers or Razor.
- Skipping tests for translation preservation.

#### 9.2.12 Historical bad data and short-term mitigation

If historical rows contain serialized `LangStr` JSON text in a plain string field, a short-term compatibility layer may normalize that payload to the current localized display value before rendering.

Current example:
- `App.BLL/ManagementUsers/ManagementUserAdminService.cs` contains logic that attempts to deserialize suspicious JSON-like string values before presenting them in the UI.

This is a temporary mitigation only.

Required long-term fix:
- Store localizable fields as `LangStr`.
- Persist them correctly as `jsonb`.
- Update them with `SetTranslation(...)`.
- Normalize bad historical rows so display code no longer depends on compatibility parsing.

### 9.3 Resource file rules for static UI and validation text

Resource files in `App.Resources` are the source of truth for translated static UI text.

Use resources for:
- page titles
- headings
- field labels
- button labels
- select option placeholder text
- validation messages
- success and error messages added in controllers or services
- reusable scaffold text and shared layout text

Relevant examples:
- `App.Resources/Views/UiText.resx`
- `App.Resources/Views/UiText.et.resx`
- `App.Resources/Views/AdminScaffoldText.resx`
- `App.Resources/Views/AdminScaffoldText.et.resx`

#### 9.3.1 ViewModel annotation rules

ViewModels must use resource-backed data annotation attributes for user-visible labels and validation messages.

Good reference examples:
- `WebApp/ViewModels/Onboarding/LoginViewModel.cs`
- `WebApp/ViewModels/Onboarding/RegisterViewModel.cs`
- `WebApp/ViewModels/Onboarding/CreateManagementCompanyViewModel.cs`

Required pattern:
- `[Display(Name = nameof(UiText.Email), ResourceType = typeof(UiText))]`
- `[Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]`
- other validation attributes should also point to localized resource entries when they show user-visible messages

Contributor rules:
- Do not leave `[Required]` or `[StringLength]` without resource-backed error messages on user-facing forms.
- Do not hardcode label text in attributes when a resource key exists or should exist.
- When adding a new field label or validation message, add entries for both English and Estonian resources.

This rule directly addresses bugs in `plans/localization/localization-bugs.md` where onboarding form labels and validation messages stayed in English.

#### 9.3.2 Controller and service message rules

User-visible messages returned from controllers and services must come from resources unless the text is truly internal and never shown to the user.

Reference examples:
- success and error messages in `WebApp/Controllers/OnboardingController.cs`
- success and error messages in `WebApp/Areas/Management/Controllers/UsersController.cs`

Required pattern:
- `TempData["OnboardingSuccess"] = App.Resources.Views.UiText.RegistrationSuccessfulPleaseLogin;`
- `ModelState.AddModelError(string.Empty, App.Resources.Views.UiText.InvalidEmailOrPassword);`
- `TempData["ManagementUsersSuccess"] = App.Resources.Views.UiText.CompanyUserUpdatedSuccessfully;`

Contributor rules:
- Avoid inline English strings for messages that can surface in UI.
- If a BLL service returns a user-visible message, prefer returning a resource-backed message or a code that the web layer maps to a resource.
- Keep the same message key reused across views and controllers where the wording should stay consistent.

This rule directly addresses bugs in `plans/localization/localization-bugs.md` where join-request and retry messages appeared in English under Estonian culture.

#### 9.3.3 Resource maintenance rules

When adding a new user-facing text key:
- add it to the English resource file
- add its Estonian translation counterpart
- use the generated strongly typed resource member where available
- keep naming consistent and intention-revealing

When changing wording:
- update all relevant culture files together
- avoid leaving stale untranslated entries in one culture

#### 9.3.4 Select lists and derived UI text

If a select list or menu item is static UI text, it belongs in resources.
If a select list item is persisted business data, it belongs in `LangStr` and should be rendered through `ToString()`.

Examples:
- `Select role` placeholder text belongs in resources.
- management company role labels loaded from DB belong in `LangStr` and are rendered as `Label.ToString()`.

This distinction directly addresses bugs from `plans/localization/localization-bugs.md` involving unlocalized `Select Role` entries and mixed static and persisted display text.

### 9.4 End-to-end localization checklist

For any localization-related change, verify all applicable layers:
- request localization configuration resolves the correct current culture
- resource files contain both English and Estonian entries
- view models use resource-backed display and validation attributes
- controllers and services do not emit hardcoded user-visible strings
- persisted multilingual fields use `LangStr`
- create flows map current-culture input into `LangStr`
- edit flows use `SetTranslation(...)` on existing entities
- views render `LangStr` through `ToString()`
- no raw JSON or dictionary payload is shown in UI
- tests cover fallback, translation preservation, and localized validation output where relevant

### 9.5 Bug patterns from past localization issues

The bug history in `plans/localization/localization-bugs.md` should be treated as a contributor checklist of failure modes to avoid.

Recurring bug classes include:
- form field labels not using resources
- validation messages not using localized resource-backed attributes
- controller or service error messages returned in English while Estonian culture is active
- static page chrome missing resource coverage
- `LangStr` dictionaries rendered directly instead of localized display strings
- edit flows overwriting all translations instead of updating only current culture
- static placeholder entries such as `Select Role` left untranslated

Before merging localization-related work, explicitly check that none of these bug classes were reintroduced.

### 9.6 Minimal verification checklist for PRs touching localization

- Create in `en` stores English translation.
- Edit in Estonian updates only the Estonian translation key and preserves English.
- Switching UI culture changes rendered `LangStr` values in list and details pages.
- Missing translation falls back according to `LangStr` behavior.
- Database stores multilingual JSON in `jsonb` for `LangStr` fields.
- New form labels use resource-backed `[Display]` attributes.
- New validation messages are localized through resource-backed data annotations.
- Controller and service messages shown to users are localized.
- No raw serialized `LangStr` payload appears anywhere in UI.
- Resource keys added for one culture are added for the other supported culture as well.

### 9.7 Seeding and lookup-label rules

Lookup tables from `Docs/schema.sql` use stable invariant `CODE` values and localizable labels.

Contributor rules:
- keep `CODE` values immutable
- keep labels localized in `LangStr`
- keep seeding idempotent by `CODE`
- keep seed examples and real seeded payloads aligned with current `LangStr` key behavior

Under current implementation, examples should follow neutral-key storage such as `en` and `et` unless and until `LangStr` behavior itself changes.

### 9.8 Real bug example: raw LangStr dictionary shown in UI

Observed production-style symptom:
- UI showed serialized dictionary or JSON payload instead of localized value, for example a job title displaying a multilingual payload rather than the current-culture text.

Root cause pattern to avoid:
- Treating localized content as a plain `string` that stores serialized `LangStr` JSON text.
- Rendering that `string` directly in Razor.

Incorrect pattern:

```csharp
entity.JobTitle = JsonSerializer.Serialize(new LangStr("Manager"));
@item.JobTitle
```

Correct pattern:

```csharp
public LangStr JobTitle { get; set; } = new();
entity.JobTitle = vm.JobTitle;
entity.JobTitle.SetTranslation(vm.JobTitle);
@item.JobTitle.ToString()
```

The important rule is not the exact syntax above, but the end-to-end behavior:
- persisted multilingual data uses `LangStr`
- create maps the current-culture string into `LangStr`
- edit updates only the active translation
- UI renders the localized display value rather than raw payload text

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
- Avoid using ViewBag, ViewData. TempData used to pass confirmation messages, error info, or small data across a redirect is fine.
- Use strongly typed ViewModels always for MVC views and controller actions unless a framework constraint makes that impossible.
- Do not use `[Bind]` if at all possible. Prefer dedicated strongly typed ViewModels with explicit mapping instead.

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
