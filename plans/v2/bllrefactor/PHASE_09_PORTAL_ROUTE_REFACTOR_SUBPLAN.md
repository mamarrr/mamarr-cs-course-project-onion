# Phase 09: Portal Route Refactor

Goal: make Portal routes more explicit and route-first without changing BLL
behavior or database constraints.

Scope:
- Replace compressed nested route segments with readable equivalents:
  - `/m/{companySlug}/c/{customerSlug}` -> `/m/{companySlug}/customers/{customerSlug}`
  - `/p/{propertySlug}` -> `/properties/{propertySlug}`
  - `/u/{unitSlug}` -> `/units/{unitSlug}`
- Update Portal controller route attributes.
- Update hardcoded navigation, breadcrumbs, and Razor links that generate those
  route shapes.
- Keep current BLL query contracts and tenant/IDOR checks unchanged.

Explicit deferrals:
- Company-scoped property shortcuts (`/m/{companySlug}/properties/{propertySlug}`)
  need either company-unique property slugs or an ambiguity policy because the
  database currently enforces property slug uniqueness per customer.
- Company-scoped unit shortcuts (`/m/{companySlug}/units/{unitSlug}`) need either
  company-unique unit slugs or an ambiguity policy because the database currently
  enforces unit slug uniqueness per property.
- Root resident shortcuts (`/resident`, `/resident/units`, `/resident/tickets`)
  need a route-first resident context selection design because current BLL flows
  require company slug and resident id code.

Out of scope:
- No DAL, schema, migration, Base, API, Admin, or test changes.
- No BLL contract changes.
- No removal of legacy behavior outside explicit route-template updates.

