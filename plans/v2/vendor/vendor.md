# Management Vendor Workflow Plan

## Summary

Add a management-company vendor workspace: managers can list, filter, open, create, edit, and manage vendors inside `/m/{companySlug}/vendors`. Vendor detail pages show vendor-compatible tickets, ticket categories, contacts, and scheduled work. All reads and writes are tenant-scoped by management company through BLL services and DAL queries.

## Key Changes

- Add a dedicated vendor BLL surface: `IManagementVendorService`, vendor query/command/model contracts, `IVendorRepository`, EF repository, UOW registration, MVC mapper, and `VendorsController`.
- Enable the existing management `Vendors` navigation item and route vendors under:
  - `GET /m/{companySlug}/vendors`
  - `POST /m/{companySlug}/vendors/add`
  - `GET /m/{companySlug}/vendors/{vendorId:guid}`
  - `POST /m/{companySlug}/vendors/{vendorId:guid}/edit`
  - `POST /m/{companySlug}/vendors/{vendorId:guid}/categories/add`
  - `POST /m/{companySlug}/vendors/{vendorId:guid}/contacts/add`
  - `POST /m/{companySlug}/vendors/{vendorId:guid}/tickets/assign`
  - `POST /m/{companySlug}/vendors/{vendorId:guid}/scheduled-works/add`
- Build the vendor list with search, active/all filter, and required `VendorTicketCategory` filter. Vendor rows link to the vendor detail page by `vendorId`.
- Vendor detail page uses tabs or compact sections for profile, compatible ticket categories, assigned tickets, contacts, and scheduled work.
- "Add ticket to vendor" means assigning an existing company ticket to the vendor. The selector must support search and only show tickets whose `TicketCategoryId` is in the vendor's active `VendorTicketCategories`.
- Inline vendor contact creation creates both `Contact` and `VendorContact` in one BLL command. The contact must belong to the current management company.
- Scheduled work creation requires a vendor-compatible ticket. It creates `ScheduledWork`, sets or keeps the ticket vendor, sets ticket `DueAt` from the scheduled start when needed, and moves Created or Assigned tickets to `Scheduled` when lifecycle guards pass.

## Interfaces And Behavior

- Vendor profile fields: `Name`, `RegistryCode`, localized `Notes`, `IsActive`; preserve existing `LangStr` rules and update notes with current culture without dropping other translations.
- Vendor category management links vendors to global `TicketCategory` rows through `VendorTicketCategory`; do not create tenant-specific categories.
- Repository queries must always include `ManagementCompanyId` constraints before materialization. Never fetch vendor, contact, ticket, or scheduled work by ID alone.
- Validation rules:
  - Duplicate `RegistryCode` is blocked inside the same management company.
  - Vendor category duplicate links are blocked; inactive links may be reactivated.
  - Ticket assignment rejects cross-tenant tickets and tickets with categories not active for the vendor.
  - Scheduled work rejects cross-tenant tickets, incompatible categories, missing work status, and invalid date ranges.
- No new public REST API is required for this MVC workflow.

## UI And Localization

- Add strongly typed management vendor view models; no `ViewBag`, no `[Bind]`.
- Add English and Estonian `UiText` resource entries for vendor list filters, empty states, actions, validation messages, success/error flash messages, and scheduled work/contact labels.
- Use existing management UI styling and PRG flow with `TempData` success/error messages.
- Keep the page operational and dense: vendor list, filter controls, inline add form, and detail sections should match existing management pages.

## Verification

- Do not write new tests while the repository testing override remains active.
- Run build/type-check verification after implementation.
- Manual scenarios to verify:
  - A management user can list vendors only for their company.
  - Search and `VendorTicketCategory` filter narrow the vendor list correctly.
  - Opening a vendor from the list shows only same-company tickets, contacts, categories, and scheduled work.
  - Assign-ticket search shows only same-company tickets with active vendor-compatible categories.
  - Scheduled work creation creates the schedule and auto-moves eligible Created or Assigned tickets to Scheduled.
  - Cross-tenant vendor, ticket, contact, and scheduled-work IDs return not found/forbidden without leaking existence.
