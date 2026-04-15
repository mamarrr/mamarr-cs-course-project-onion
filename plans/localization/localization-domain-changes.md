# Domain localization changes plan (`LangStr`) + UI message localization pass

## Scope

This updated plan reflects product direction:

1. **All previously listed Group A and Group B domain fields must be migrated to `LangStr`.**
2. **UI messages across the project must be localized (EN + ET) via resource-based localization.**

Baseline reference for localized domain and MVC flow remains `ContactType`.

---

## 1) Domain localization baseline already present

The following lookup/reference entities are already correctly modeled with invariant `Code` + localizable `Label: LangStr`:

- `ContactType`
- `CustomerRepresentativeRole`
- `LeaseRole`
- `ManagementCompanyRole`
- `PropertyType`
- `TicketCategory`
- `TicketPriority`
- `TicketStatus`
- `WorkStatus`

No change needed for the above besides consistency checks in EF mapping, migrations, and seed data.

---

## 2) Required `LangStr` conversions in `App.Domain` (approved)

### Group A (required now)

Convert these fields from `string`/`string?` to `LangStr`/`LangStr?`:

- `Property.Label`
- `Ticket.Title`
- `Ticket.Description`
- `VendorContact.RoleTitle`
- `ManagementCompanyUser.JobTitle`

### Group B (required now)

Convert these fields from `string?` to `LangStr?`:

- `Contact.Notes`
- `Customer.Notes`
- `CustomerRepresentative.Notes`
- `Lease.Notes`
- `Property.Notes`
- `Unit.Notes`
- `Vendor.Notes`
- `VendorTicketCategory.Notes`
- `ScheduledWork.Notes`
- `WorkLog.Description`
- `ManagementCompanyJoinRequest.Message`

---

## 3) Fields that remain invariant `string`

Keep these as plain strings (identifier/technical/legal/contact coordinate semantics):

- All lookup `Code` fields
- `Slug`, `TicketNr`, `UnitNr`
- `RegistryCode`, `VatNumber`, `IdCode`
- `Address`, `AddressLine`, `City`, `PostalCode`, `Email`, `Phone`, `BillingEmail`, `BillingAddress`, `ContactValue`
- `ManagementCompanyJoinRequest.Status`
- Canonical names: `AppUser.FirstName`, `AppUser.LastName`, `Resident.FirstName`, `Resident.LastName`, `Customer.Name`, `ManagementCompany.Name`, `Vendor.Name`, `VendorContact.FullName`

---

## 4) UI message localization pass (project scan results)

A pass over `WebApp` found many hardcoded UI strings that should be moved to resources.

### 4.1 High-priority views with hardcoded text

- Shared layouts and common chrome:
  - `WebApp/Views/Shared/_Layout.cshtml`
  - `WebApp/Views/Shared/_OnboardingLayout.cshtml`
  - `WebApp/Views/Shared/_LoginPartial.cshtml`
  - `WebApp/Views/Shared/_LanguageSelection.cshtml`
  - `WebApp/Views/Shared/Error.cshtml`
- Home and onboarding:
  - `WebApp/Views/Home/AccessDenied.cshtml`
  - `WebApp/Views/Home/Index.cshtml`
  - `WebApp/Views/Home/Privacy.cshtml`
  - `WebApp/Views/Onboarding/Index.cshtml`
  - `WebApp/Views/Onboarding/Login.cshtml`
  - `WebApp/Views/Onboarding/Register.cshtml`
  - `WebApp/Views/Onboarding/NewManagementCompany.cshtml`
  - `WebApp/Views/Onboarding/JoinManagementCompany.cshtml`
  - `WebApp/Views/Onboarding/ResidentAccess.cshtml`
- Management area UI:
  - `WebApp/Areas/Management/Views/Shared/_ManagementLayout.cshtml`
  - `WebApp/Areas/Management/Views/Dashboard/Index.cshtml`
  - `WebApp/Areas/Management/Views/Users/Index.cshtml`
  - `WebApp/Areas/Management/Views/Users/Edit.cshtml`
- Admin area scaffolds (systematic hardcoded actions/titles):
  - many files under `WebApp/Areas/Admin/Views/**` still use literal strings such as `Create`, `Edit`, `Delete`, `Back to List`, `Details`, `Index`.

### 4.2 High-priority controller/viewmodel message sources

- Onboarding flow messages:
  - `WebApp/Controllers/OnboardingController.cs`
    - `TempData` success messages
    - `ModelState.AddModelError(...)` literals
    - context chooser informational text
- Management users flow messages:
  - `WebApp/Areas/Management/Controllers/UsersController.cs`
    - `TempData` success/error
    - `ModelState.AddModelError(...)`
    - hardcoded `ViewData["Title"]`
- Onboarding and management view models with literal UI text:
  - `WebApp/ViewModels/Onboarding/ResidentAccessViewModel.cs`
  - `WebApp/ViewModels/Onboarding/JoinManagementCompanyViewModel.cs`
  - `WebApp/ViewModels/Management/Layout/ManagementLayoutViewModel.cs`
  - `WebApp/ViewModels/ManagementUsers/ManagementUsersPageViewModel.cs` (`Display(Name=...)`)

---

## 5) Required implementation workstreams

### Workstream A: Domain + persistence (`LangStr` migration)

For each approved conversion field:

1. Change domain property type to `LangStr` / `LangStr?`.
2. Ensure EF mapping stores as `jsonb` with converter.
3. Add migration(s) to transform existing columns to `jsonb` payloads.
4. Update seed/data init where needed.

### Workstream B: MVC/UI flow updates

1. Update create/edit view models to use single-culture string inputs.
2. Map create flows `string -> LangStr`.
3. Map edit flows via `SetTranslation(...)` on existing entity fields.
4. Render localized values via `ToString()` in details/index/list/table cells.

### Workstream C: Resource localization of UI messages

1. Introduce/extend resource files per area/domain/view.
2. Replace hardcoded Razor text with resource references.
3. Replace controller literal messages (`TempData`, `ModelState`, titles) with resource-backed values.
4. Replace hardcoded `[Display(Name=...)]` with resource-backed display metadata.

### Workstream D: Verification

1. EN/ET render checks for converted domain fields.
2. Edit-in-ET preserves EN translation.
3. UI message smoke pass for onboarding/shared/management/admin pages.
4. Migration and roundtrip tests for `jsonb`-stored `LangStr` fields.

---

## 6) Sequencing (recommended)

1. Implement domain conversions for Group A + Group B.
2. Implement EF conversions + migrations.
3. Update MVC create/edit/list/details for converted entities.
4. Localize shared layouts + onboarding + management pages.
5. Localize admin scaffold literals in a systematic pass.
6. Complete tests and localization verification.
