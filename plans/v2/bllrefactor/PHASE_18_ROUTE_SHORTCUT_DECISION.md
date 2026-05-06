# Phase 18 - Route Shortcut Decision

Parent plan: `BLL_WEBAPP_AKAVER_REFACTOR_PLAN.md`

## Decision

Do not implement company-scoped property/unit shortcut routes in this refactor.

The current nested Portal routes remain the accepted route design:

- `/m/{companySlug}/customers/{customerSlug}/properties/{propertySlug}`
- `/m/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/profile`
- `/m/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units`
- `/m/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}`
- `/m/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/profile`
- `/m/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/tenants`

## Rationale

Property slugs are currently scoped by customer, and unit slugs are currently scoped by property. Company-scoped shortcut routes such as `/m/{companySlug}/properties/{propertySlug}` and `/m/{companySlug}/units/{unitSlug}` would be ambiguous unless the application adds a new ambiguity policy or changes slug uniqueness rules.

Nested routing keeps the parent context explicit and preserves the existing tenant and ownership checks without schema changes.

## Future Revisit Trigger

Reconsider shortcut routes only if one of these becomes true:

- property slugs become unique per management company,
- unit slugs become unique per management company,
- the product explicitly defines an ambiguity policy for duplicate slugs.
